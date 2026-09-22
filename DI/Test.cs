namespace web_app_scratch;

public interface ITest { void Log(); }
public class Test : ITest
{
    public Test()
    {
        Console.WriteLine("[Test] constructed");
    }
    public void Log()
    {
        Console.WriteLine("[Test] Log called");
    }
}
