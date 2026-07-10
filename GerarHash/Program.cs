using BCrypt.Net;
var hash = BCrypt.HashPassword("045491d9e", 12);
Console.WriteLine(hash);
