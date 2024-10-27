// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
 // Initialize the map and set its view to a starting location and zoom level
    var map = L.map('map').setView([54.6872, 25.2797], 13); // Example: Vilnius, Lithuania

    // Load and display a map layer (you can use other providers as well)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '© OpenStreetMap contributors'
    }).addTo(map);

    // Add a click event listener on the map
    map.on('click', function(e) {
        // Get the latitude and longitude of the clicked location
        var lat = e.latlng.lat;
        var lng = e.latlng.lng;
        
        // Display the coordinates in the console or use them in other ways
        console.log("Latitude: " + lat + ", Longitude: " + lng);

        // You can use this to make a marker appear at the clicked location
        L.marker([lat, lng]).addTo(map)
            .bindPopup("Coordinates: " + lat.toFixed(5) + ", " + lng.toFixed(5))
            .openPopup();

        // Optionally, store coordinates in a hidden input to submit with a form
        document.getElementById("latitude").value = lat;
        document.getElementById("longitude").value = lng;
    });