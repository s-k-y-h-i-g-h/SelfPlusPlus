# Introduction
Self++ is a project designed to take you and add a little bit more to improve you and your capabilities past what is normally possible.

This isn't just for healthy people - if you are suffering from health problems then we intend to help you to overcome them too.

This project is OSS! It has been published using the GPL-3.0 license (see: [LICENSE](LICENSE)).

Website: https://s-k-y-h-i-g-h.github.io/SelfPlusPlus/

# Tools
This project comes with software tools for biohackers!

## Building
```
dotnet build .\SelfPlusPlus.sln -c Release
```

## Included Programs
### Log
CLI program which logs your activites and stores them in a portable and transferable JSON format (Samsung Health also exports data in JSON).

#### Usage
Print help message:
```
.\bin\Log.exe
```

Log drinking a cup of coffee:
```
.\bin\Log.exe --Action Add --Type Consumption --Category Substance --Name "Filter Coffee" --Amount "2 tbsp"
```

Log consuming a stack:
```
.\bin\Log.exe --Action Add --Type Consumption --Category Stack --Name "Morning Stack"
```

Log your vitals:
```
.\bin\Log.exe --Action Add --Type Measurement --Category Vitals --Name "BP" --value "130/80"
.\bin\Log.exe --Action Add --Type Measurement --Category Vitals --Name "HR" --value "70"
```