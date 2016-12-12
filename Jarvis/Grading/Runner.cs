using System;
using System.Diagnostics;
using System.Text;
using HtmlDiff;
using System.IO;
using System.Timers;
using System.Collections.Generic;
using System.Net.Mail;
using System.IO.Compression;
using System.Threading;

namespace Jarvis
{
  public class Runner
  {
    public RunResult Run(Assignment homework)
    {
      RunResult result = new RunResult(homework);
      string testsPath = string.Format("{0}/courses/{1}/tests/hw{2}/", Jarvis.Config.AppSettings.Settings["workingDir"].Value, homework.Course, homework.HomeworkId); 

      if (File.Exists(testsPath + "config.xml"))
      {
        List<TestCase> testCases = Utilities.ReadTestCases(testsPath + "config.xml");

        // Style check
        Logger.Info("Running style check on {0} {1}", homework.StudentId, homework.HomeworkId);
        // result.StyleMessage = StyleCheck(homework);

        // Not ready for this yet
        result.JarvisStyleMessage = "Coming&nbsp;soon!";

        // Compile
        Logger.Info("Compiling {0} {1}", homework.StudentId, homework.HomeworkId);
        result.CompileMessage = Compile(homework, testCases[0].ProvidedSourceFiles);

        // Run tests
        if (result.CompileMessage == "Success!!")
        {
          Logger.Info("Running {0} {1}", homework.StudentId, homework.HomeworkId);
          result.OutputMessage = RunAllTestCases(testCases, homework, result);

          // Delete binary
          File.Delete(homework.Path + homework.StudentId);
        }
        else
        {
          result.OutputMessage = "<p>Didn't compile... :(</p>";
        }

        // Write result into results file, writes a new entry for each run
        RecordResult(homework, result);
      }
      else
      {
        result.OutputMessage = "<p>Sir, I cannot find any test case configurations for this assignment. Is your assignment number correct?<p>";
      }

      return result;
    }

    private void RecordResult(Assignment homework, RunResult result)
    {
      string timestamp = DateTime.Now.ToString();

      using (StreamWriter writer = new StreamWriter(homework.Path + "results.txt", true))
      {
        writer.WriteLine(timestamp + " " + homework.StudentId + " " + result.Grade); 
        writer.Flush();
        writer.Close();
      }
    }

    private string JarvisStyleCheck(Assignment homework)
    {
      StyleExecutor executor = new StyleExecutor();

      string errors = "";

      foreach (string file in homework.FileNames)
      {
        errors += executor.Run(homework.Path + file);
      }

      return JarvisEncoding.ToHtmlEncoding(errors);
    }

    private string StyleCheck (Assignment homework)
    {
      string result = string.Empty;
      using (Process p = new Process())
      {
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.WorkingDirectory = homework.Path;

        string styleExe = Jarvis.Config.AppSettings.Settings["styleExe"].Value;
        p.StartInfo.FileName = styleExe;
        p.StartInfo.Arguments = Jarvis.Config.AppSettings.Settings["styleExemptions"].Value;
        foreach (string file in homework.FileNames)
        {
          p.StartInfo.Arguments += " " + homework.Path + file;
        }

        Logger.Trace("Style checking with {0} and arguments {1}", styleExe, p.StartInfo.Arguments);

        p.Start();
        Jarvis.StudentProcesses.Add(p.Id);

        result = p.StandardError.ReadToEnd();
        result = result.Replace(homework.Path, "");
        result = JarvisEncoding.ToHtmlEncoding(result);
        p.WaitForExit();

        p.Close();
      }

      return result;
    }

    private string Compile(Assignment homework, List<string> providedFiles)
    {
      // Find all C++ source files
      List<string> sourceFiles = new List<string>();
      foreach (string file in homework.FileNames)
      {
        if (file.EndsWith(".cpp") || file.EndsWith(".cxx") || file.EndsWith(".cc"))
        {
          sourceFiles.Add(file);
        }
      }

      // Add in provided files
      string testsPath = string.Format("{0}/courses/{1}/tests/hw{2}/", Jarvis.Config.AppSettings.Settings["workingDir"].Value, homework.Course, homework.HomeworkId);
      foreach (string file in providedFiles)
      {
        // Copy all provided files (headers and source)
        Logger.Trace("Copying {0} to {1}", testsPath + file, homework.Path);
        File.Copy(testsPath + file, homework.Path + file, true);

        // Only build source files
        if (file.EndsWith(".cpp") || file.EndsWith(".cxx") || file.EndsWith(".cc"))
        {
          sourceFiles.Add(file);
        }
      }

      string result = "";
      using (Process p = new Process())
      {
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.FileName = "g++";
        p.StartInfo.Arguments = "-DJARVIS -std=c++11 -Werror -o" + homework.Path + homework.StudentId + " -I" + homework.Path;
        foreach (string source in sourceFiles)
        {
          //string temp = source.Replace("_", "\_");
          p.StartInfo.Arguments += string.Format(" \"{0}{1}\"", homework.Path, source);
        }

        p.Start();

        Logger.Trace("Compilation string: {0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);

        Jarvis.StudentProcesses.Add(p.Id);

        result = p.StandardError.ReadToEnd();
        result = result.Replace(homework.Path, "");
        result = JarvisEncoding.ToHtmlEncoding(result);

        p.WaitForExit();

        p.Close();
      }
      Logger.Trace("Compile result: {0}", result);

      return (!string.IsNullOrEmpty(result)) ? result : "Success!!";
    }

    private string RunAllTestCases(List<TestCase> tests, Assignment homework, RunResult grade)
    {
      string testsPath = string.Format("{0}/courses/{1}/tests/hw{2}/", Jarvis.Config.AppSettings.Settings["workingDir"].Value, homework.Course, homework.HomeworkId);
      StringBuilder result = new StringBuilder();
      int passingTestCases = 0;

      foreach (TestCase test in tests)
      {
        Logger.Info("Running test case {0}", test.Id);
        string stdInput = string.Empty;

        // clear out any previously created input/output files
        DirectoryInfo dir = new DirectoryInfo(homework.Path);
        foreach (FileInfo file in dir.GetFiles())
        {
          if (!file.Name.Contains(homework.StudentId) && !file.Name.Equals("results.txt"))
          {
            file.Delete(); 
          }
        }

        // check for std input file
        if (!string.IsNullOrEmpty(test.StdInputFile))
        {
          stdInput = testsPath + test.StdInputFile;
        }

        // check for file input files
        foreach (InputFile filein in test.FileInputFiles)
        {
          File.Copy(testsPath + filein.CourseFile, homework.Path + filein.StudentFile, true);
        }

        // Execute the program
        test.StdOutText = ExecuteProgram(homework, stdInput);
        test.Duration = homework.Duration;

        string output = test.GetResults(homework.Path, testsPath);
        result.AppendLine(output);

        if (test.Passed)
        {
          passingTestCases++;
        }
      }

      grade.OutputPercentage = passingTestCases / (double)tests.Count;

      return result.ToString();
    }

    private string ExecuteProgram(Assignment homework, string inputFile)
    {      
      string output = string.Empty;
      string input = string.Empty;
      if (File.Exists(inputFile))
      {
        using (StreamReader reader = new StreamReader(inputFile))
        {
          input = reader.ReadToEnd();

          reader.Close();
        }
      }

      using (Process executionProcess = new Process())
      {
        executionProcess.StartInfo.WorkingDirectory = homework.Path;
        executionProcess.StartInfo.UseShellExecute = false;
        executionProcess.StartInfo.RedirectStandardOutput = true;
        executionProcess.StartInfo.RedirectStandardError = true;
        executionProcess.StartInfo.RedirectStandardInput = true;

        if (!File.Exists(homework.Path + homework.StudentId))
        {
          Logger.Fatal("Executable " + homework.Path + homework.StudentId + " did not exist!!");
          output = "[Jarvis could not find the executable!]";
        }
        else
        {
          StringBuilder outputStr = new StringBuilder();

          using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
          {
            executionProcess.OutputDataReceived += (sender, e) =>
            {
              if (e.Data == null)
              {
                outputWaitHandle.Set();
              }
              else
              {
                outputStr.AppendLine(e.Data);
              }
            };

            DateTime startTime = DateTime.Now;
            DateTime finishTime;
            executionProcess.StartInfo.FileName = homework.Path + homework.StudentId;
            executionProcess.Start();
            executionProcess.BeginOutputReadLine();

            try
            {
              executionProcess.StandardInput.AutoFlush = true;
              if (!string.IsNullOrEmpty(input))
              {
                executionProcess.StandardInput.Write(input);
              }

              Jarvis.StudentProcesses.Add(executionProcess.Id);

              if (executionProcess.WaitForExit(15000) && outputWaitHandle.WaitOne(15000))
              {
                // Process completed
                finishTime = DateTime.Now;
                homework.Duration = finishTime - startTime;

                output = outputStr.ToString();
              }
              else
              {
                // Timed out
                finishTime = DateTime.Now;
                homework.Duration = finishTime - startTime;

                executionProcess.Kill();
                output += "\n[Unresponsive program terminated by Jarvis]\n";
              }
            }
            catch (Exception e)
            {
              Logger.Fatal("Fatal exception while running program");
              Logger.Fatal(e.ToString());
            }
            executionProcess.Close();
          }
        }
      }
      
      return output;
    }
  }
}

