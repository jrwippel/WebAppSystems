#r "nuget: BCrypt.Net-Next, 4.0.3"
var hash = BCrypt.Net.BCrypt.HashPassword("045491d9e", 12);
Console.WriteLine(hash);
