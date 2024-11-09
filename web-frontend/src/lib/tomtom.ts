import type { Map, LngLatLike } from '@tomtom-international/web-sdk-maps';
import type { Point } from './store';
import type { CalculateRouteResponse } from '@tomtom-international/web-sdk-services';

export let apiKey = "empty";
export let language = "lt-LT";

export async function updateRouteLayer(map: Map, points: Point[]) {
    const tt = await import('@tomtom-international/web-sdk-maps');
    const { services } = await import('@tomtom-international/web-sdk-services');

    const locations: LngLatLike[] = points.map((p) => [p.longtitude, p.latitude]);
    const routeData = await services.calculateRoute({
        key: apiKey,
        locations,
        travelMode: 'bicycle',
    });

    const routeSource = map.getSource('route')
    if (routeSource === undefined) {
        map.addSource('route', {
            type: 'geojson',
            data: routeData.toGeoJson()
        })
    } else if (routeSource.type === 'geojson') {
        routeSource.setData(routeData.toGeoJson())
    } else {
        throw Error('Invalid source type, expected geojson')
    }

    if (!map.getLayer('route')) {
        map.addLayer({
            id: 'route',
            type: 'line',
            source: 'route',
            paint: {
                'line-color': '#4a90e2',
                'line-width': 5
            }
        });
    }

    return routeData
}

export async function centerOnRoute(map: Map, routeData: CalculateRouteResponse) {
    const tt = await import('@tomtom-international/web-sdk-maps');
    const bounds = new tt.LngLatBounds();
    for (const point of routeData.routes.flatMap((r) => r.legs).flatMap((leg) => leg.points)) {
        bounds.extend([point.lng as number, point.lat as number]);
    }
    map.fitBounds(bounds, { padding: 120, duration: 0 });
}

export async function createMarkers(map: Map, points: Point[]) {
    const tt = await import('@tomtom-international/web-sdk-maps');
    const markers = []
    for (const point of points) {
        const marker = new tt.Marker();
        marker.setLngLat([point.longtitude, point.latitude]);
        marker.addTo(map);
        markers.push(marker)
    }
    return markers
}