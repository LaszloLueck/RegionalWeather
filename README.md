# RegionalWeather
## Description
See current state for a short briefing about the final solution.
- Written in C#
- Runs in a Docker-Container
- Stores data in elastic
- visualization? Don´t know, think kibana or grafana.
- in any case dotnet core 5.x / c# 9


...

## How it looks like (very prototypish)
Here you have a look a the grafana dashboard looks like. Above the temperature of 20 selected locations around me plotted as a time series. The second Panel is a Worldmap item that shows the current temperatures in the map.
Not bad for first prototype, but a lot of work to do...
<img src="images/prototype.png" witdh="1024"><img/>

## Current State
Currently this project is a lot more as an collection of ideas and a bunch of code.
I imagine the following:
- Reading of current weather information via OpenWeatherMap (https://openweathermap.org/) via its API --> check
- Full amount of locations can be configured. --> check, realized per file
- In order of your selected OWM-plan, please be aware of the amount of API-calls (e.g. free plan  = max. 1.000.000 calls per month / max. 60 calls per min.).
- The current weather information is called up every minute (configurable) --> check, currently it runs every 5 min.
- And stored with the corresponding geographic information in an ES instance. --> check, it works, really!
- With Grafana or another tool, the weather information is made visible on a website (e.g. heat maps, graphs or similar). --> check, looks not bad.
- The storage takes place continuously, which means that historical views are also possible. --> check, it works by default with elastic.

## What currently work (in some parts, not as a complete project)
- Store the plain results of owm as file. Reason: For later possibility of complete reload historic data in a (possible new) elastic index. Also if i made changes in index- or data-settings we could use this, because currently i store not all data from owm to es.
- Read out the weather informations for configurable places
- convert the json to appropriate objects in c#
- establish a connection to one or more elasticsearch instances
- create an index
- create a mapping
- check if the index exists
- delete the index
- mount the locations file outside of the container so it is fully configurable from the host.
- store the weather information to elastic
- dashboard (very prototypish)
- containerization (runs as docker container on my server)

## Whats next?

- transform the original weather-to to an appropriated elastic to (only fields that i need)
- travis functions
- sonar functions
- testcoverage

