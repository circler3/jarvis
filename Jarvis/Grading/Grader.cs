using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.IO.Compression;
using System.Threading;
using System.Collections.Concurrent;

namespace Jarvis
{
  public class Grader
  {
    private const int POOL_SIZE = 20;
    private List<Thread> threadPool = new List<Thread>();

    private ConcurrentQueue<Assignment> toBeGradedQueue = new ConcurrentQueue<Assignment>();
    private ConcurrentQueue<RunResult> resultQueue = new ConcurrentQueue<RunResult>();

    private string currentAssignment;
    private string currentCourse;

    private bool runMoss = true;
    private bool runCode = true;

    public Grader(bool runMoss, bool runCode)
    {
      this.runMoss = runMoss;  
      this.runCode = runCode;

      // Create thread pool workers
      for (int i = 0; i < POOL_SIZE; ++i)
      {
        threadPool.Add(new Thread(new ThreadStart(AssignmentRunnerWorker)));
      }
    }

    public string Grade(string baseDir, List<Assignment> assignments)
    {            
      // extract to temp directory
      // parse headers
      Logger.Trace("Extracting grader zip file");
      string gradingReport = string.Empty;

      // copy to course directory structure
      currentAssignment = assignments[0].HomeworkId;
      currentCourse = assignments[0].Course;
      string hwPath = string.Format("{0}/courses/{1}/hw{2}/", baseDir, currentCourse, currentAssignment);

      string[] sections = Directory.GetDirectories(hwPath, "section*", SearchOption.AllDirectories);
      foreach (string section in sections)
      {
        if (File.Exists(section + "/grades.txt"))
        {
          File.Delete(section + "/grades.txt");
        }
      }

      Logger.Info("Grading {0} assignments for course: {1} - HW#: {2}", assignments.Count, currentCourse, currentAssignment);

      foreach (Assignment a in assignments)
      {
        if (a.ValidHeader)
        {
          string oldPath = a.Path;
          a.Path = string.Format("{0}section{1}/{2}/", hwPath, a.LabSection, a.StudentId);

          Directory.CreateDirectory(a.Path);

          foreach (string file in a.FileNames)
          {
            if (File.Exists(a.Path + file))
            {
              File.Delete(a.Path + file);
            }

            Logger.Trace("Moving {0} to {1}", oldPath + file, a.Path + file);
            File.Move(oldPath + file, a.Path + file);
          }
        }

        toBeGradedQueue.Enqueue(a);
      }
      string mossResponse = string.Empty;
      // Check MOSS before grading so we don't have to wait for grading to find out if MOSS fails
      if (runMoss)
      {        
        mossResponse = SendToMoss(hwPath, currentCourse, currentAssignment);

        if (string.IsNullOrEmpty(mossResponse))
        {
          mossResponse = "Moss didn't respond!";
        }
        else
        {
          mossResponse = "<a href='" + mossResponse + "'>" + mossResponse + "</a><br />";
        }

        // add MOSS URL to result
        gradingReport = "<a href='" + mossResponse + "'>" + mossResponse + "</a><br />";
      }
      else
      {
        gradingReport = "MOSS skipped.<br />";
      }

      if (runCode)
      {
        Logger.Info("Starting {0} grading threads to grade {1} assignments", POOL_SIZE, toBeGradedQueue.Count);

        // Start worker pool
        foreach (Thread t in threadPool)
        {
          t.Start();
        }

        Logger.Info("Waiting for threads to complete");
        // Wait for all threads to exit
        foreach (Thread t in threadPool)
        {
          t.Join();
        }

        Logger.Info("Threads completed! {0} results received", resultQueue.Count);

        gradingReport += SendFilesToSectionLeaders(hwPath, currentCourse, currentAssignment);

        string graderEmail = File.ReadAllText(hwPath + "../grader.txt");

        Logger.Info("Sending Canvas CSV to {0}", graderEmail);

        CanvasFormatter canvasFormatter = new CanvasFormatter();

        string gradesPath = canvasFormatter.GenerateCanvasCsv(hwPath, currentAssignment, resultQueue.ToArray());

        Utilities.SendEmail(graderEmail,
          "Grades for " + currentCourse + " " + currentAssignment,
          "Hello! Attached are the grades for " + currentCourse + " " + currentAssignment + ". Happy grading!\n" + mossResponse,
          gradesPath);
      }
      else
      {
        gradingReport += "Execution skipped.<br />";
      }
      // Generate some kind of grading report
      return gradingReport;
    }

    private void AssignmentRunnerWorker()
    {
      Assignment assignment = null;

      while (!toBeGradedQueue.IsEmpty)
      {
        int remaining = toBeGradedQueue.Count;
        Logger.Info("=== {0} remaining assignments to grade", remaining);
        if (toBeGradedQueue.TryDequeue(out assignment))
        {
          if (assignment.ValidHeader && assignment.HomeworkId == currentAssignment)
          {
            Logger.Info("~~~GRADING: {0}, HW: {1}", assignment.StudentId, assignment.HomeworkId);
            // run grader on each file and save grading result
            Runner runner = new Runner();
            RunResult result = runner.Run(assignment);
            resultQueue.Enqueue(result);
          }
          else
          {
            RunResult result = new RunResult(assignment);
            resultQueue.Enqueue(result);
          }
        }
      }
    }

    private string SendToMoss(string hwPath, string currentCourse, string currentHomework)
    {
      // Submit all files to MOSS
      string mossId = Jarvis.Config.AppSettings.Settings["mossId"].Value;

      // Find all *.cpp files in hw directory
      string[] cppFiles = Directory.GetFiles(hwPath, "*.cpp", SearchOption.AllDirectories);

      Logger.Info("Submitting {0} files to MOSS", cppFiles.Length);
      // create moss interface
      MossInterface moss = new MossInterface
      {
        UserId = Int32.Parse(mossId), 
        Language = "cc", // C++
        NumberOfResultsToShow = 500,
        Comments = string.Format("USU - Jarvis - {0} - HW {1}", currentCourse, currentHomework),
      };

      // add files to interface
      moss.Files = new List<string>(cppFiles);

      // submit request
      string mossReponse = string.Empty;
      if (moss.SendRequest(out mossReponse))
      {
        Logger.Info("Moss returned success! {0}", mossReponse);
      }
      else
      {
        mossReponse = "";
        Logger.Warn("Moss submission unsuccessful");
      }

      return mossReponse;
    }

    private string SendFilesToSectionLeaders(string hwPath, string currentCourse, string currentHomework)
    {
      // zip contents
      // email to section leader
      Logger.Info("Sending files to section leaders");
      string[] directories = Directory.GetDirectories(hwPath, "section*", SearchOption.AllDirectories);
      StringBuilder gradingReport = new StringBuilder();
      gradingReport.AppendLine("<p>");
      foreach (string section in directories)
      {
        Logger.Trace("Processing section at {0}", section);
        string sectionNumber = section.Substring(section.LastIndexOf("section"));
        string zipFile = string.Format("{0}/../{1}.zip", section, sectionNumber);

        Logger.Trace("Creating {0} zip file at {1}", sectionNumber, zipFile);
        // zip contents
        if (File.Exists(zipFile))
        {
          File.Delete(zipFile);
        }

        ZipFile.CreateFromDirectory(section, zipFile);

        if (File.Exists(section + "/leader.txt"))
        {
          string leader = File.ReadAllText(section + "/leader.txt");

          Logger.Trace("Emailing zip file to {0}", leader);

          // attach to email to section leader
          Utilities.SendEmail(leader, 
            "Grades for " + currentCourse + " " + currentHomework,
            "Hello! Attached are the grades for " + currentCourse + " " + currentHomework + ". Happy grading!",
            zipFile);        

          gradingReport.AppendLine(string.Format("Emailed section {0} grading materials to {1} <br />", sectionNumber, leader));
        }
        else
        {
          gradingReport.AppendLine(string.Format("Couldn't find section leader for section {0}<br/>", sectionNumber));
        }
      }

      gradingReport.AppendLine("</p>");

      return gradingReport.ToString();
    }
  }
}

