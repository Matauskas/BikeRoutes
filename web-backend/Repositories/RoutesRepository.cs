using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using web_backend;
using web_backend.Entities;
using web_backend.Repositories;

public class RoutesRepository
{
    private readonly BaseDbContext db;
    private readonly PointsRepository points;

    public RoutesRepository(BaseDbContext context, PointsRepository points)
    {
        db = context ?? throw new ArgumentNullException(nameof(context));
        this.points = points;
    }

    public int? SaveRoute(Route route)
    {
        int? pointList = points.Create();
        Debug.Assert(pointList != null);

        DbRoute dbRoute = new DbRoute
        {
            OwnerId = route.OwnerId,
            Title = route.Title,
            PointListId = pointList.Value,
            Time = route.Time,
            Distance = route.Distance
        };
        db.Routes.Add(dbRoute);

        db.SaveChanges();

        if (!points.Update(dbRoute.PointListId, route.Points))
        {
            return null;
        }

        db.SaveChanges();

        return dbRoute.Id;
    }

    public bool Update(int id, UpdateRouteOptions opts)
    {
        var route = db.Routes.Find(id);
        if (route == null)
        {
            return false;
        }

        if (opts.Title != null)
        {
            route.Title = opts.Title;
        }
        if (opts.Distance != null)
        {
            route.Distance = (double)opts.Distance;
        }
        if (opts.Time != null)
        {
            route.Time = (double)opts.Time;
        }

        if (opts.points != null)
        {
            if (!points.Update(route.PointListId, opts.points))
            {
                return false;
            }
        }

        db.SaveChanges();

        return true;
    }

    public bool Delete(int id)
    {
        // TODO: Should delete point list which is associated with the given route
        throw new NotImplementedException();
    }

    private static Route ConvertFromDb(PointsRepository pointsRepo, DbRoute route)
    {
        return new Route
        {
            Id = route.Id,
            OwnerId = route.OwnerId,
            Title = route.Title,
            Points = pointsRepo.ListPoints(route.PointListId),
            Distance = route.Distance,
            Time = route.Time,
        };
    }

    public async Task<List<Route>> List()
    {
        return await db.Routes
            .Select(route => ConvertFromDb(this.points, route))
            .ToListAsync();
    }

    public async Task<List<Route>> ListByUser(int userId)
    {
        return await db.Routes
            .Where(route => route.OwnerId == userId)
            .Select(route => ConvertFromDb(this.points, route))
            .ToListAsync();
    }
}