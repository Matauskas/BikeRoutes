using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using web_backend.Entities;
using Microsoft.Azure.Documents.SystemFunctions;

namespace web_backend
{
    public class BaseDbContext : DbContext
    {
        private readonly ILogger logger;

        public BaseDbContext(DbContextOptions<BaseDbContext> options, ILogger<BaseDbContext> logger) : base(options)
        {
            this.logger = logger;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<DbRoute> Routes { get; set; }
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<DbPointList> PointLists { get; set; }
        public DbSet<DbPoint> Points { get; set; }
        public DbSet<Review> Reviews { get; set; }


        public void Initialize()
        {
            Database.EnsureCreated();

            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS PointLists (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT
                )");
            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Points (
                    ListId     INTEGER,
                    PointIndex INTEGER,
                    Longtitude REAL,
                    Latitude   REAL,
                    FOREIGN KEY('ListId') REFERENCES 'PointLists'('Id') ON DELETE CASCADE
                )");
            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Users (
                    Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username  TEXT,
                    Password  TEXT,
                    FirstName TEXT,
                    LastName  TEXT,
                    Email     TEXT,
                    PhotoUrl  TEXT,
                    Latitude  TEXT, 
                    Longitude TEXT,

                    TotalDistance REAL DEFAULT 0,
                    TotalTime     REAL DEFAULT 0,
                    TotalCount    INTEGER DEFAULT 0
                )");
            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Routes (
                    Id          INTEGER,
                    Title       TEXT,
                    OwnerId     INTEGER,
                    PointListId INTEGER,
                    Distance    REAL DEFAULT 0,
                    Time        REAL DEFAULT 0,
                    FOREIGN KEY('OwnerId') REFERENCES 'Users'('Id') ON DELETE CASCADE,
                    FOREIGN KEY('PointListId') REFERENCES 'PointLists'('Id'),
                    PRIMARY KEY('Id' AUTOINCREMENT)
                )");
            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Friendships (
                    UserA           INTEGER,
                    UserB  	        INTEGER,
                    FOREIGN KEY('UserA') REFERENCES 'Users'('Id') ON DELETE CASCADE,
                    FOREIGN KEY('UserB') REFERENCES 'Users'('Id') ON DELETE CASCADE
                )");
            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS FriendRequests (
                    FromUser INTEGER,
                    ToUser   INTEGER,
                    FOREIGN KEY('FromUser') REFERENCES 'Users'('Id') ON DELETE CASCADE,
                    FOREIGN KEY('ToUser') REFERENCES 'Users'('Id') ON DELETE CASCADE
                )");
            Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Reviews (
                    Id  INTEGER,
                    UserId INTEGER,
                    RouteId INTEGER,
                    Rating INTEGER,
                    Description TEXT,
                    FOREIGN KEY('UserId') REFERENCES 'Users'('Id') ON DELETE CASCADE,
                    FOREIGN KEY('RouteId') REFERENCES 'Routes'('Id') ON DELETE CASCADE,
                    PRIMARY KEY('Id' AUTOINCREMENT)
                )");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Friendship>().HasKey(req => new { req.UserA, req.UserB });
            modelBuilder.Entity<FriendRequest>().HasKey(req => new { req.FromUser, req.ToUser });
            modelBuilder.Entity<DbPoint>().HasKey(req => new { req.ListId, req.PointIndex });
        }
    }

}
