using System;
using System.Collections.Generic;
using System.Text;

namespace Jarvis
{
  public class AnimationViewer : IViewer
  {
    private class Keyword
    {
      public Keyword(string word)
      {
        Word = word;
        Found = false;
      }

      public string Word { get; set; }
      public bool Found { get; set; }
    }

    private List<Keyword> keywords = new List<Keyword>();
    private string keywordList;
    #region Script
    private const string script1 = @"
<script type='text/javascript' class='animation'> ";

    private const string script2 = @"
var currentFrame{0} = 0;
var timer{0} = null;
var timerDelay{0} = 1000;

play{0} = function()
{{
  var playButton{0} = document.getElementById('playButton{0}');

  if (playButton{0}.value == '>')
  {{
    playButton{0}.value = '||';
    timer{0} = window.setTimeout(nextFrame{0}, timerDelay{0});
  }}
  else
  {{
    playButton{0}.value = '>';
    window.clearInterval(timer{0});
  }}
}}

nextFrame{0} = function()
{{
  var loopBox{0} = document.getElementById('loopCheckBox{0}');
  var screen{0} = document.getElementById('screen{0}');

  var currentFrameBox{0} = document.getElementById('currentFrameBox{0}');
  currentFrameBox{0}.value = currentFrame{0};

  screen{0}.innerHTML = frames{0}[currentFrame{0}];
  currentFrame{0} = (currentFrame{0} + 1);

  if (currentFrame{0} >= frames{0}.length)
  {{
    currentFrame{0} = 0;

    if (loopCheckBox{0}.checked)
    {{
      timer{0} = window.setTimeout(nextFrame{0}, timerDelay{0});
    }}
    else
    {{
      document.getElementById('playButton{0}').value = '>';
    }}
  }}
  else
  {{
    timer{0} = window.setTimeout(nextFrame{0}, timerDelay{0});
  }}
}}

slower{0} = function()
{{
  timerDelay{0} *= 2;

  var speedBox{0} = document.getElementById('speedBox{0}');
  speedBox{0}.value = (1000 / timerDelay{0}) + 'x';
}}

faster{0} = function()
{{
  var speedBox{0} = document.getElementById('speedBox{0}');
  timerDelay{0} /= 2;

  speedBox{0}.value = (1000 / timerDelay{0}) + 'x';
}}//# sourceURL=animation.js
</script> ";

    string script3 = @"
<div style='margin-left: 35px; outline: solid; height: 41em; width: 51em;'><p style='margin-left: 0px;' id='screen{0}'></p></div>
<br />
<div id='controls{0}' style='margin-left: 35px;'>
  <label for='currentFrameBox{0}'>Frame:</label>
  <input type='text' value='0' id='currentFrameBox{0}' size='1'/>
  <label for='speedBox{0}'>Speed:</label>
  <input type='text' value='1x' id='speedBox{0}' size='1' />
  <label for='loopCheckBox{0}'>Loop:</label>
  <input type='checkbox' id='loopCheckBox{0}' />
  <br />
  <input type='button' onclick='slower{0}();' value='<<' />
  <input type='button' onclick='play{0}();' id='playButton{0}' value='>' />
  <input type='button' onclick='faster{0}();' value='>>' />
</div> ";
    #endregion

    public AnimationViewer(string words)
    {
      keywordList = words;
      string[] splitWords = words.Split(',');

      foreach (string word in splitWords)
      {
        string trimedWord = word.Trim();
        keywords.Add(new Keyword(trimedWord));
      }
    }

    public string ToHtml(TestCase data)
    {
      Logger.Trace("Viewing output as Animation");

      string[] splitters = { "=~=" };
      string[] frames = data.StdOutText.Split(splitters, StringSplitOptions.None);
      frames = RemoveEmptyFrames(frames);

      StringBuilder scriptFramesVar = new StringBuilder("var frames{0} = [");
      for (int i = 0; i < frames.Length; ++i)
      {
        foreach (Keyword keyword in keywords)
        {
          if (frames[i].Contains(keyword.Word))
          {
            keyword.Found = true;
          }
        }

        string htmlFrame = Utilities.ToHtmlEncoding(frames[i].TrimEnd(' '));
        scriptFramesVar.AppendFormat("\"{0}\"", htmlFrame);

        if (i < frames.Length - 1)
        {
          scriptFramesVar.Append(",");
        }
      }

      scriptFramesVar.Append("]; ");

      bool foundAllKeywords = true;
      foreach (Keyword keyword in keywords)
      {
        if (!keyword.Found)
        {
          foundAllKeywords = false;
          break; // Once we've missed one, then we don't need to keep looking
        }
      }

      data.Passed = foundAllKeywords;

      StringBuilder builder = new StringBuilder();
      builder.AppendFormat(script1, data.Id);
      builder.AppendFormat(scriptFramesVar.ToString(), data.Id);
      builder.AppendFormat(script2, data.Id);
      builder.AppendFormat("<p>Searched for: \"{0}\"</p> ", keywordList);
      builder.AppendFormat(script3, data.Id);

      return Utilities.BuildDiffBlock("Animation:", builder.ToString(), "", "");
    }

    private string[] RemoveEmptyFrames(string[] frames)
    {
      List<string> newFrames = new List<string>();

      foreach (string frame in frames)
      {
        string newFrame = frame.TrimEnd(' ');

        if (newFrame != "\n")
        {
          newFrames.Add(newFrame);
        }
      }

      return newFrames.ToArray();
    }
  }
}

