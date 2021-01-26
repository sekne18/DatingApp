namespace API.Entities
{
    public class AppUser
    {
        public int Id { get; set; }      //naming is important for entity
        public string UserName { get; set; }    //naming is important for entity
        
        public byte[] PasswordHash { get; set; }

        public byte[] PasswordSalt { get; set; }
    }
}