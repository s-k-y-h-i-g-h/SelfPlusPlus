# Brief

Command-line interface for Self++. This gives users the ability to track their biohacking activities and shows them useful information and provides AI-powered analysis and advice.

This uses the Spectre CLI framework and [Rapid Console .NET template](https://github.com/jasontaylordev/RapidConsole).

# Data
## User Log Entries (LogData.json)
### Location
By default this is stored in the root of SelfPlusPlus which is in the application user data directory (platform dependent) in the user's home directory.

### File Format
An array of JSON objects, each with an ISO 8601 timestamp and details about the event. There are different types of events.

# Commands
## Add
## Delete
## Edit
## Show
### Options
#### --format
**json**: Displays the entries in JSON format.