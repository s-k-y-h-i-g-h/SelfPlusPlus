---
title: "Software"
layout: single
---

This project comes with software tools for biohackers!

# Log
CLI (Command-Line Interface) program which logs your activites. This is intended for all the biohackers who also happen to be nerds.

## Data Location
Data is stored in the standard application data subdirectory in the user's home directory.

On Windows: 
```
C:\Users\[USERNAME]\AppData\Local\SelfPlusPlus\Log.json
```

## Usage
Print help message:
```
.\bin\Release\Log.exe
```

Output:
```
Usage:
  Log --action Add --type <Consumption|Measurement> --category <...> --name <Name> [other params]
  Log --action Update --timestamp <ISO8601 or local> [fields to change]
  Log --action Remove --timestamp <ISO8601 or local>

Parameters:
  --action     Add | Update | Remove
  --type       Consumption | Measurement (required for Add, optional for Update)
  --category   For Consumption: Substance | Stack. For Measurement: Vitals
  --name       Entry name (required for Add, optional for Update)
  --amount     String amount (required for Consumption:Substance)
  --value      Float value (required for Measurement)
  --unit       Unit string (required for Measurement)
  --timestamp  Optional for Add; if given, used as event time. Required for Update/Remove
```

Log drinking a cup of coffee:
```
.\bin\Release\Log.exe --action Add --type Consumption --category Substance --name "Filter Coffee" --amount "2 tbsp"
```

Log consuming a stack:
```
.\bin\Release\Log.exe --action Add --type Consumption --category Stack --name "Morning Stack"
```

Log your vitals:
```
.\bin\Release\Log.exe --action Add --type Measurement --category Vitals --name "Systolic BP" --unit mmHg --value 130
.\bin\Release\Log.exe --action Add --type Measurement --category Vitals --name "Diastolic BP" --unit mmHg --value 80
.\bin\Release\Log.exe --action Add --type Measurement --category Vitals --name HR --value 70 --unit BPM
```

Update/remove a log entry:

This currently requires opening the log file and copying the timestamp for the entry then passing it to the program like this (which makes these actions kind of useless at the moment because you could easily just remove the entry from the log file yourself when you have it open):
```
.\bin\Release\Log.exe --action Update --timestamp "2025-09-22T15:33:19.1659460+00:00" --amount "400mg"
.\bin\Release\Log.exe --action Remove --timestamp "2025-09-22T15:33:19.1659460+00:00"
 ```