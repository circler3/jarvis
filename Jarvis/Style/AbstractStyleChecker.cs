using System;

namespace Jarvis
{
  public abstract class AbstractStyleChecker
  {
    protected StyleExecutor executor;

    protected AbstractStyleChecker(StyleExecutor executor)
    {
      this.executor = executor;
    }

    public abstract void Check(string line);
  }
}

