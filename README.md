# Introduction
Self++ is a project designed to take you and add a little bit more to improve you and your capabilities past what is normally possible.

This isn't just for healthy people - if you are suffering from health problems then we intend to help you to overcome them too.

This project is OSS! It has been published using the GPL-3.0 license (see: [LICENSE](LICENSE)).

Website: https://s-k-y-h-i-g-h.github.io/SelfPlusPlus/

# Tools
This project comes with software tools for biohackers!

## Building
### Development Environment
Set up your development environment with:
```
.\devenv\bootstrap.ps1
```

And build the project with:
```
.\build.ps1
```

### Production
```
dotnet build .\SelfPlusPlus.sln -c Release
```

## Included Programs
### Log
CLI program which logs your activites and stores them in a portable and transferable JSON format (Samsung Health also exports data in JSON).

#### Usage
Print help message:
```
.\bin\Release\Log.exe
```

Output:
```
Usage:
  Log --Action Add --Type <Consumption|Measurement> --Category <...> --Name <Name> [other params]
  Log --Action Update --Timestamp <ISO8601 or local> [fields to change]
  Log --Action Remove --Timestamp <ISO8601 or local>

Parameters:
  --Action     Add | Update | Remove
  --Type       Consumption | Measurement (required for Add, optional for Update)
  --Category   For Consumption: Substance | Stack. For Measurement: Vitals
  --Name       Entry name (required for Add, optional for Update)
  --Amount     String amount (required for Consumption:Substance)
  --Value      Float value (required for Measurement)
  --Unit       Unit string (required for Measurement)
  --Timestamp  Optional for Add; if given, used as event time. Required for Update/Remove
```

Log drinking a cup of coffee:
```
.\bin\Release\Log.exe --Action Add --Type Consumption --Category Substance --Name "Filter Coffee" --Amount "2 tbsp"
```

Log consuming a stack:
```
.\bin\Release\Log.exe --Action Add --Type Consumption --Category Stack --Name "Morning Stack"
```

Log your vitals:
```
.\bin\Release\Log.exe --Action Add --Type Measurement --Category Vitals --Name "Systolic BP" -Unit mmHg --Value 130
.\bin\Release\Log.exe --Action Add --Type Measurement --Category Vitals --Name "Diastolic BP" -Unit mmHg --Value 80
.\bin\Release\Log.exe --Action Add --Type Measurement --Category Vitals --Name HR --Value 70 -Unit BPM
```

Update/remove a log entry:

This currently requires opening the log file and copying the timestamp for the entry then passing it to the program like this (which makes these actions kind of useless at the moment because you could easily just remove the entry from the log file yourself when you have it open):
```
.\bin\Release\Log.exe --Action Update -Timestamp "2025-09-22T15:33:19.1659460+00:00" -Amount "400mg"
.\bin\Release\Log.exe --Action Remove -Timestamp "2025-09-22T15:33:19.1659460+00:00"
 ```