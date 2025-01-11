# My Energy

This repo contains the code for myenergy app. The app is hosted at [https://sujithq.github.io/myenergy/](https://sujithq.github.io/myenergy/)

It is a simple app that shows the energy consumption of my household. The data is provided by [June Energy](https://www.june.energy/) and [ISolarCloud](https://www.isolarcloud.com/). The data is refreshed every hour between 05-23.

It is also ingesting data from [MeteoStat](https://meteostat.net/) to show the weather data.

I always had to check June Energy and ISolarCloud app to see how much energy I have used and generated. I also had to check the weather app to see if there are anomalies. I wanted to see all this information in one place. So I created this app.

## Data
### June Data Refresh Scheduled
[![June Data Refresh Scheduled](https://github.com/sujithq/myenergy/actions/workflows/JuneData.yml/badge.svg?branch=main&event=schedule)](https://github.com/sujithq/myenergy/actions/workflows/JuneData.yml)

### June Data Refresh Dispatched
[![June Data Refresh Dispatched](https://github.com/sujithq/myenergy/actions/workflows/JuneData.yml/badge.svg?branch=main&event=workflow_dispatch)](https://github.com/sujithq/myenergy/actions/workflows/JuneData.yml)

### Sungrow Data Refresh Scheduled
[![Sungrow Data Refresh Scheduled](https://github.com/sujithq/myenergy/actions/workflows/SungrowData.yml/badge.svg?branch=main&event=schedule)](https://github.com/sujithq/myenergy/actions/workflows/SungrowData.yml)

### Sungrow Data Refresh Dispatched
[![June Data Refresh Dispatched](https://github.com/sujithq/myenergy/actions/workflows/SungrowData.yml/badge.svg?branch=main&event=workflow_dispatch)](https://github.com/sujithq/myenergy/actions/workflows/SungrowData.yml)


### MeteoStat Data Refresh Scheduled
[![MeteoStat Data Refresh Scheduled](https://github.com/sujithq/myenergy/actions/workflows/MeteoStatData.yml/badge.svg?branch=main&event=schedule)](https://github.com/sujithq/myenergy/actions/workflows/MeteoStatData.yml)

### MeteoStat Data Refresh Dispatched
[![MeteoStat Data Refresh Dispatched](https://github.com/sujithq/myenergy/actions/workflows/MeteoStatData.yml/badge.svg?branch=main&event=workflow_dispatch)](https://github.com/sujithq/myenergy/actions/workflows/MeteoStatData.yml)

### Charge Data Refresh Scheduled
[![Charge Data Refresh Scheduled](https://github.com/sujithq/myenergy/actions/workflows/Charge.yml/badge.svg?branch=main&event=schedule)](https://github.com/sujithq/myenergy/actions/workflows/Charge.yml)

### Charge Data Refresh Dispatched
[![Charge Data Refresh Dispatched](https://github.com/sujithq/myenergy/actions/workflows/Charge.yml/badge.svg?branch=main&event=workflow_dispatch)](https://github.com/sujithq/myenergy/actions/workflows/Charge.yml)


### Sun Rise/Set Data Refresh Scheduled
[![Sun Rise/Set Data Refresh Scheduled](https://github.com/sujithq/myenergy/actions/workflows/SunRiseSet.yml/badge.svg?branch=main&event=schedule)](https://github.com/sujithq/myenergy/actions/workflows/SunRiseSet.yml)

### Sun Rise/Set Data Refresh Dispatched
[![Sun Rise/Set Data Refresh Dispatched](https://github.com/sujithq/myenergy/actions/workflows/SunRiseSet.yml/badge.svg?branch=main&event=workflow_dispatch)](https://github.com/sujithq/myenergy/actions/workflows/SunRiseSet.yml)


## Deployment
### Pages Deployment
[![pages-build-deployment](https://github.com/sujithq/myenergy/actions/workflows/pages/pages-build-deployment/badge.svg?branch=gh-pages)](https://github.com/sujithq/myenergy/actions/workflows/pages/pages-build-deployment)

## Analysis

### CodeQL

[![CodeQL Advanced](https://github.com/sujithq/myenergy/actions/workflows/codeql.yml/badge.svg)](https://github.com/sujithq/myenergy/actions/workflows/codeql.yml)

### June Energy

I needed the following information to use their API. (Found when logged in on the site)

- `client_id`
- `client_secret`
- `username`
- `password`

To get data, I had to use the following endpoints.

https://api.june.energy/rest/oauth/token

https://api.june.energy/eliq/contract/{contractid}/summary?from={from}&to={to}&valueType={valueType}

### ISolarCloud

I needed the following information to use their API. (Found when logged in on the site)

- `APP_RSA_PUBLIC_KEY`
- `ACCESS_KEY`
- `APP_KEY`
- `PS_ID`
- `username`
- `gatewayUrl`

To get data, I had to use the following endpoints.

https://gateway.isolarcloud.eu/v1/userService/login

https://gateway.isolarcloud.eu/v1/powerStationService/getHouseholdStoragePsReport

### MeteoStat

I needed the following information to use their API. (Found when logged in on the site)

- `Host`
- `Station`
- `Key` (using)

To get data, I had to use the following endpoints.

https://meteostat.p.rapidapi.com/stations/daily?station={settings.Station}&start={start}&end={end}&lat=50.96352&lon=4.60589

https://gateway.isolarcloud.eu/v1/powerStationService/getHouseholdStoragePsReport

