using System.Text.RegularExpressions;

private string pattern = @"^(?=.{1,90}$)(?:build|feat|ci|chore|docs|fix|perf|refactor|revert|style|test)(?:\(.+\))*!?(?::).{4,}(?:#\d+)*(?<![\.\s])$";
private string msg = File.ReadAllLines(Args[0])[0];

if (Regex.IsMatch(msg, pattern))
   return 0;

Console.ForegroundColor = ConsoleColor.Red;
Console.Error.WriteLine("Invalid commit message");
Console.ResetColor();
Console.Error.WriteLine("e.g: 'feat(scope): subject' or 'fix: subject'");
Console.ForegroundColor = ConsoleColor.Gray;
Console.Error.WriteLine("more info: https://www.conventionalcommits.org/en/v1.0.0/");

return 1;