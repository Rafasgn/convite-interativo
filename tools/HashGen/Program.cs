if (args.Length != 1)
{
    Console.Error.WriteLine("Uso: dotnet run --project tools/HashGen -- <senha>");
    return 1;
}

Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(args[0]));
return 0;
