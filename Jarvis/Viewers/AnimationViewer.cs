using System;
using System.Collections.Generic;

namespace Jarvis
{
  public class AnimationViewer : IViewer
  {
    private List<string> keywords = new List<string>();

    public AnimationViewer(string words)
    {
      string[] wordArray = words.Split(',');

      foreach (string word in wordArray)
      {
        keywords.Add(word);
      }
    }

    public string ToHtml(TestCase data)
    {
      string result = @"
<script>
var frames = ['a', '<br />b', '<br /><br />c'];
var currentFrame = 0;
var timer = null;
var timerDelay = 1000;

function play()
{
  var playButton = document.getElementById('playButton');

  if (playButton.value == '>')
  {
    playButton.value = '||';
    timer = window.setTimeout(nextFrame, timerDelay);
  }
  else
  {
    playButton.value = '>';
    window.clearInterval(timer);
  }
}

function nextFrame()
{
  var screen = document.getElementById('screen');

  var currentFrameBox = document.getElementById('currentFrameBox');
  currentFrameBox.value = currentFrame;

  screen.innerHTML = frames[currentFrame];
  currentFrame = (currentFrame + 1);

  if (currentFrame >= frames.length)
  {
    document.getElementById('playButton').value = '>';
    currentFrame = 0;
  }
  else
  {
    timer = window.setTimeout(nextFrame, timerDelay);
  }
}

function slower()
{
  timerDelay *= 2;

  var speedBox = document.getElementById('speedBox');
  speedBox.value = (1000 / timerDelay) + 'x';
}

function faster()
{
  timerDelay /= 2;


  var speedBox = document.getElementById('speedBox');
  speedBox.value = (1000 / timerDelay) + 'x';
}
</script>
<div id='screen' style='outline: solid; height: 10em; width: 10em;'></div>
<div id='controls'>
  <label for='currentFrameBox'>Frame:</label>
  <input type='text' value='0' id='currentFrameBox' size='1'/>
  <label for='speedBox'>Speed:</label>
  <input type='text' value='1x' id='speedBox' size='1' />
  <br />
  <input type='button' onclick='slower();' value='<<' />
  <input type='button' onclick='play();' id='playButton' value='>' />
  <input type='button' onclick='faster();' value='>>' />
</div>";
      

      return result;
    }

    /*
     
     */
  }
}

