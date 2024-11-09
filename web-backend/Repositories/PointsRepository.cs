using web_backend.Entities;

namespace web_backend.Repositories;

public class PointsRepository
{
    private readonly BaseDbContext db;

    public PointsRepository(BaseDbContext context)
    {
        db = context;
    }

    public List<Point> ListPoints(int id)
    {
        return db.Points
            .Where(point => point.ListId == id)
            .OrderBy(p => p.PointIndex)
            .Select(p => new Point { Latitude = p.Latitude, Longtitude = p.Longtitude })
            .ToList();
    }

    public bool Delete(int id)
    {
        var list = db.PointLists.Find(id);
        if (list == null) return false;

        db.PointLists.Remove(list);
        return db.SaveChanges() > 0;
    }

    public bool Update(int id, List<Point> points)
    {
        var existingPoints = ListPoints(id);

        if (existingPoints.Count > points.Count)
        {
            for (int i = points.Count; i < existingPoints.Count; i++)
            {
                db.Points.Remove(new DbPoint { ListId = id, PointIndex = i });
            }
        }

        for (int i = 0; i < Math.Min(points.Count, existingPoints.Count); i++)
        {
            var existingPoint = existingPoints[i];
            var updatedPoint = points[i];
            if (existingPoint.Longtitude != updatedPoint.Longtitude && existingPoint.Latitude != updatedPoint.Latitude)
            {
                db.Update(new DbPoint {
                    ListId = id,
                    PointIndex = i,
                    Longtitude = points[i].Longtitude,
                    Latitude = points[i].Latitude
                });
            }
        }

        if (points.Count > existingPoints.Count)
        {
            for (int i = existingPoints.Count; i < points.Count; i++)
            {
                db.Points.Add(new DbPoint {
                    ListId = id,
                    PointIndex = i,
                    Latitude = points[i].Latitude,
                    Longtitude = points[i].Longtitude
                });
            }
        }

        db.SaveChanges();

        return true;
    }

    public int? Create(List<Point>? points = null)
    {
        var list = new DbPointList();
        db.PointLists.Add(list);
        db.SaveChanges();

        if (points != null)
        {
            for (var i = 0; i < points.Count; i++)
            {
                db.Points.Add(new DbPoint {
                    ListId = list.Id,
                    PointIndex = i,
                    Latitude = points[i].Latitude,
                    Longtitude = points[i].Longtitude
                });
            }
        }

        return list.Id;
    }
}
