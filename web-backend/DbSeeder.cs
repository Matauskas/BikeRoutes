using Microsoft.AspNetCore.StaticFiles;
using System.Diagnostics;
using web_backend.Entities;
using web_backend.Repositories;

namespace web_backend
{
    public class DbSeeder
    {
        private UserRepository users;
        private RoutesRepository routes;
        private FriendsRepository friends;
        private BaseDbContext db;

        public DbSeeder(UserRepository users, RoutesRepository routes, FriendsRepository friends, BaseDbContext db)
        {
            this.users = users;
            this.routes = routes;
            this.db = db;
            this.friends = friends;
        }

        public User CreateUserIfNew(User user)
        {
            var existingUser = users.GetUserByUsername(user.Username);
            if (existingUser != null)
            {
                return existingUser;
            }

            Debug.Assert(users.RegisterUser(user));
            return user;
        }

        private static double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        private static double Haversine(double lon1_deg, double lat1_deg, double lon2_deg, double lat2_deg)
        {
            const double r = 6378100 / 1000; // kilometers

            var lat1 = ConvertToRadians(lat1_deg);
            var lon1 = ConvertToRadians(lon1_deg);
            var lat2 = ConvertToRadians(lat2_deg);
            var lon2 = ConvertToRadians(lon2_deg);

            var sdlat = Math.Sin((lat2 - lat1) / 2);
            var sdlon = Math.Sin((lon2 - lon1) / 2);
            var q = sdlat * sdlat + Math.Cos(lat1) * Math.Cos(lat2) * sdlon * sdlon;
            var d = 2 * r * Math.Asin(Math.Sqrt(q));

            return d;
        }

        private static double GetDistanace(List<Point> points)
        {
            double total = 0;
            for (int i = 0; i < points.Count-1; i++)
            {
                total += Haversine(points[0].Longtitude, points[0].Latitude, points[1].Longtitude, points[1].Latitude);
            }
            return total;
        }

        public int CreateRouteIfNew(Route route)
        {
            var existingRoute = db.Routes.Where(r => r.Title == route.Title && r.OwnerId == route.OwnerId).FirstOrDefault();
            if (existingRoute != null)
            {
                return existingRoute.Id;
            }

            var bycicleSpeedKmh = 27;
            route.Distance = GetDistanace(route.Points);
            route.Time = (route.Distance / 1000) / bycicleSpeedKmh;

            var routeId = routes.SaveRoute(route);
            Debug.Assert(routeId != null);
            return routeId.Value;
        }

        public List<Point> RandomPointList(double longtitude, double latitude, int size = 2, float distance = 1)
        {
            var rng = new Random();

            var points = new List<Point>();
            for (int i = 0; i < size; i++)
            {
                longtitude += (rng.NextDouble()*2-1) * distance;
                latitude   += (rng.NextDouble()*2-1) * distance;
                points.Add(new Point { Longtitude = longtitude, Latitude = latitude });
            }
            return points;
        }

        public void EnsureFriends(int userA, int userB)
        {
            if (!friends.AreFriends(userA, userB))
            {
                friends.CreateFriends(userA, userB);
            }
        }

        public void Initialize()
        {
            var admin = CreateUserIfNew(new User
            {
                Username = "admin",
                Password = "admin",
                FirstName = "Adminius",
                LastName = "Administrauskas",
                Email = "admin@best-email.com"
            });

            var alice = CreateUserIfNew(new User
            {
                Username = "alice",
                Password = "alice",
                FirstName = "Alisa",
                LastName = "Alisienė",
                Email = "alice@best-email.com"
            });

            var bob = CreateUserIfNew(new User
            {
                Username = "bob",
                Password = "bob",
                FirstName = "Bobas",
                LastName = "Bobauskas",
                Email = "bob@best-email.com"
            });

            var charlie = CreateUserIfNew(new User
            {
                Username = "charlie",
                Password = "charlie",
                FirstName = "Čiarlis",
                LastName = "Čiarliauskas",
                Email = "charlie@best-email.com"
            });

            EnsureFriends(admin.Id, alice.Id);
            EnsureFriends(admin.Id, bob.Id);
            EnsureFriends(alice.Id, bob.Id);

            var kaunasLatitude = 54.896870;
            var kaunasLongtitude = 23.892429;

            CreateRouteIfNew(new Route
            {
                OwnerId = alice.Id,
                Title = "Pragaro kelias (alice)",
                Points = RandomPointList(kaunasLongtitude, kaunasLatitude, 2)
            });

            CreateRouteIfNew(new Route
            {
                OwnerId = admin.Id,
                Title = "Pragaro kelias (admin)",
                Points = RandomPointList(kaunasLongtitude, kaunasLatitude, 5, 1f/5)
            });

        }
    }
}
