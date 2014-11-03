River-Net
=========

A peformant distributed .NET service for scheduling Elasticsearch ETLs. It uses Quartz.Net for scheduling and runs in IIS (for the time being).


##Documentation
* [Setup](#setup)
* [API](#api)
* [River Sources](#river_sources)
* [Deployment](#deployment)

<a name="setup"/>
### Setup
Under River.Quartz there is a [quartz.sql](https://github.com/CompassPHS/River-Net/blob/master/River.Quartz/quartz.sql) file. This will create a database called ```RiverNet```, a schema called ```quartz```, and the necessary objects to support [Quartz.NET](https://github.com/quartznet/quartznet "").
The SQL Server instance is specified in the [connectionString](https://github.com/CompassPHS/River-Net/blob/master/River.Api/Web.config#L58) in the quartz section of the web.config under River.Api.

<a name="api"/>
### API
Jobs are created through ```PUT``` requests to ```/api/river/{job_name}```

A typical job looks like this:
```javascript
{
  "name":"job_name", 
  "cron":"0 */2 * * *"
  "suppressNulls":true,
  "source":{
    "type":"database",
	"connectionString":"Server=server_name;Database=db_name;Connection Timeout=30;Trusted_Connection=True;",
     "command":"exec river_SomeJob"      
  },
  "destination":{
    "url":"http://localhost:9200",
    "index":"index",
    "type":"type"
  }
}

```

```"cron"``` is optional. Excluding a cron will run the job immediately and once only.

```"suppressNulls"``` will exclude any fields with the value ```NULL``` from the output document.

<a name="river_sources"/>
### River Sources
There are two kinds of sources: Database and FlatFile

Here's a Database source:
```javascript
{
  "type":"database",
  "connectionString":"Server=server_name;Database=db_name;Connection Timeout=30;Trusted_Connection=True;",
  "command":"exec river_SomeJob"      
}
```

Here's a FlatFile source:
```javascript
{
  "type":"flatFile",
  "location":"c:\path\to\file",
  "delimiters":[',']
}
```

<a name="deployment"/>
### Deployment
This app runs in IIS and is subject to the idle timeout and recycling any other IIS site would have.

