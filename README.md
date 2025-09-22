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
# Requires that you run it as Administrator
.\devenv\bootstrap.ps1
```

And build the project with:
```
.\build.ps1
```

You can run Jekyll locally to test changes to the website with:
```
.\run.ps1
```

Development URL: http://127.0.0.1:4000/SelfPlusPlus/

### Production
```
dotnet build .\SelfPlusPlus.sln -c Release
```

## Included Programs
### Log
CLI program which logs your activites and stores them in a portable and transferable JSON format (Samsung Health also exports data in JSON).

Software documentation is [here](site/software.md).