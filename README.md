# RegionalWeather
## Description
See current state for a short briefing about the final solution.
- Written in C#
- Runs in a Docker-Container
...
## Current State
Currently this project ist a collection of ideas. 
I imagine the following:
- Reading of current weather information via OpenWeatherMap (https://openweathermap.org/) via its API
- Up to 20 locations can be configured.
- The current weather information is called up every minute (configurable)
- And stored with the corresponding geographic information in an ES instance.
- With Grafana or another tool, the weather information is made visible on a website (e.g. heat maps, graphs or similar).
- The storage takes place continuously, which means that historical views are also possible.
