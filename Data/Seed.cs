using System.Collections.Generic;
using DatingApp.API.Models;
using Newtonsoft.Json;

namespace DatingApp.API.Data
{
    public class Seed
    {
        public DataContext _context { get; set; }
        public Seed(DataContext context)
        {
            _context = context;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using(var hmac = new System.Security.Cryptography.HMACSHA512()){
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }


        public void SeedUsers(){
            //_context.Users.RemoveRange(_context.Users);
            //_context.SaveChanges();

            var userData = System.IO.File.ReadAllText("Data/UserSeedData.json");
            var users = JsonConvert.DeserializeObject<List<User>>(userData);
            users.ForEach(x => {
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash("password",out passwordHash, out passwordSalt);

                x.PasswordHash = passwordHash;
                x.PasswordSalt = passwordSalt;

                x.Username = x.Username.ToLower();

                _context.Users.Add(x);
            });
            
            _context.SaveChanges();
        }
    }
}