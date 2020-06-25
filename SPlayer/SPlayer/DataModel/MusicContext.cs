namespace SPlayer.DataModel
{
    using System;
    using System.Data.Entity;
    using System.Linq;

    public class MusicContext : DbContext
    {
        public MusicContext()
            : base("name=MusicContext")
        {
        }
        public DbSet<Music> Musics { get; set; }
    }
}