var map;
var dotnetRef;
var vehicles = {};

window.initMap = function (dotNetReference) {
    dotnetRef = dotNetReference;

    // Center on Seychelles
    map = L.map('map').setView([-4.6796, 55.4920], 10);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap'
    }).addTo(map);
}

// Create a vehicle object and routing control
function createVehicle(vehicleId, color) {

    vehicles[vehicleId] = {
        waypoints: [],
        markers: [],
        movingMarker: null,
        routeCoords: [],
        intervalId: null, // store animation interval
        routingControl: L.Routing.control({
            waypoints: [],
            lineOptions: {
                styles: [{ color: color, weight: 5 }]
            },
            routeWhileDragging: false,
            addWaypoints: false,
            draggableWaypoints: false,
            createMarker: function () {
                return null; // removes default blue markers
            }
        }).addTo(map)
    };

    // Route created event
    vehicles[vehicleId].routingControl.on('routesfound', function (e) {
        var route = e.routes[0];

        // Store route coordinates for animation
        vehicles[vehicleId].routeCoords = route.coordinates.map(c => L.latLng(c.lat, c.lng));

        var distance = (route.summary.totalDistance / 1000).toFixed(2);
        var time = (route.summary.totalTime / 60).toFixed(0);

        dotnetRef.invokeMethodAsync('UpdateRouteInfo', vehicleId, distance, time);

        // Automatically start looping animation
        let markerColor = vehicles[vehicleId].markers.length > 0 
            ? vehicles[vehicleId].markers[0].options.icon.options.color || color 
            : color;

        startVehicleLoop(vehicleId, markerColor);
    });
}

// Search coordinates in Seychelles only
async function getCoords(placeName) {
    let query = placeName + ", Seychelles";

    let url = "https://nominatim.openstreetmap.org/search?format=json&limit=1&countrycodes=sc&q="
        + encodeURIComponent(query);

    let response = await fetch(url, {
        headers: {
            'Accept': 'application/json',
            'User-Agent': 'SNYCTransportApp'
        }
    });

    let data = await response.json();

    if (data.length > 0) {
        return [data[0].lat, data[0].lon];
    }
}

// Add a numbered marker for a vehicle
window.addVehiclePoint = async function (vehicleId, color, placeName) {

    if (!vehicles[vehicleId]) {
        createVehicle(vehicleId, color);
    }

    let coords = await getCoords(placeName);
    if (!coords) return;

    let stopNumber = vehicles[vehicleId].waypoints.length + 1;

    let marker = L.marker(coords, {
        icon: L.divIcon({
            className: '',
            html: `
                <div style="
                    background:${color};
                    width:32px;
                    height:32px;
                    border-radius:50%;
                    text-align:center;
                    line-height:32px;
                    color:white;
                    font-weight:bold;
                    border:2px solid white;
                    font-size:14px;">
                    ${stopNumber}
                </div>
            `,
            iconSize: [32, 32]
        }),
        draggable: false
    }).addTo(map);

    // Store marker
    vehicles[vehicleId].markers.push(marker);

    // Store waypoint
    vehicles[vehicleId].waypoints.push(
        L.latLng(coords[0], coords[1])
    );

    // Update route
    vehicles[vehicleId].routingControl.setWaypoints(vehicles[vehicleId].waypoints);

    map.setView(coords, 10);
}

// Clear vehicle route and markers
window.clearVehicleRoute = function (vehicleId) {

    if (vehicles[vehicleId]) {

        // Remove all markers
        vehicles[vehicleId].markers.forEach(m => map.removeLayer(m));
        vehicles[vehicleId].markers = [];

        // Clear waypoints
        vehicles[vehicleId].waypoints = [];
        vehicles[vehicleId].routingControl.setWaypoints([]);

        // Remove routing control
        map.removeControl(vehicles[vehicleId].routingControl);

        // Remove moving marker if exists
        if (vehicles[vehicleId].movingMarker)
            map.removeLayer(vehicles[vehicleId].movingMarker);

        // Stop any animation interval
        if (vehicles[vehicleId].intervalId) {
            clearInterval(vehicles[vehicleId].intervalId);
            vehicles[vehicleId].intervalId = null;
        }

        // Delete vehicle
        delete vehicles[vehicleId];
    }
}

// Fix map size on window resize
window.fixMapSize = function () {
    if (map) map.invalidateSize();
}

// Create a car icon for animation
function getCarIcon(color) {
    return L.divIcon({
        className: '',
        html: `<div style="transform: rotate(0deg); font-size:24px;">🚗</div>`,
        iconSize: [30,30]
    });
}

// Animate vehicle along its route continuously (loops)
function startVehicleLoop(vehicleId, color) {
    let v = vehicles[vehicleId];
    if (!v || !v.routeCoords || v.routeCoords.length === 0)
        return;

    // Stop previous animation if any
    if (v.intervalId) {
        clearInterval(v.intervalId);
        v.intervalId = null;
    }
    if (v.movingMarker)
        map.removeLayer(v.movingMarker);

    let i = 0;
    v.movingMarker = L.marker(v.routeCoords[0], { icon: getCarIcon(color) }).addTo(map);

    v.intervalId = setInterval(() => {
        if (i >= v.routeCoords.length) {
            i = 0; // loop back to start
        }
        v.movingMarker.setLatLng(v.routeCoords[i]);
        i++;
    }, 40); // adjust speed
}

updated