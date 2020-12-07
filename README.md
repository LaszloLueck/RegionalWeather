# RegionalWeather
## Description
See current state for a short briefing about the final solution.
- Written in C#
- Runs in a Docker-Container
- Stores data in elastic
- visualization? Don´t know, think kibana or grafana.
- in any case dotnet core 5.x / c# 9


...
## Current State
Currently this project ist a collection of ideas and a bunch of code.
I imagine the following:
- Reading of current weather information via OpenWeatherMap (https://openweathermap.org/) via its API
- Full amount of locations can be configured.
- In order of your selected OWM-plan, please be aware of the amount of API-calls (e.g. free plan  = max. 1.000.000 calls per month / max. 60 calls per min.).
- The current weather information is called up every minute (configurable)
- And stored with the corresponding geographic information in an ES instance.
- With Grafana or another tool, the weather information is made visible on a website (e.g. heat maps, graphs or similar).
- The storage takes place continuously, which means that historical views are also possible.

## What currently work (in some parts, not as a complete project)
- Read out the weather informations for configurable places
- convert the json to appropriate objects in c#
- establish a connection to one or more elasticsearch instances
- create an index
- create a mapping
- check if the index exists
- delete the index

## Whats next?
- mount the locations file outside of the container so it is fully configurable from the host.
- store the weather information to elastic
- transform the original weather-to to an appropriated elastic to (only fields that i need).
