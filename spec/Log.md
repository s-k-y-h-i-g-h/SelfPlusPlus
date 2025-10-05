Command-line C#/.NET program which lets the user log health and biohacking related things.

# Data
## Location
By default this is stored in the root of SelfPlusPlus which is in the application user data directory (platform dependent) in the user's home directory.

## File Format
An array of JSON objects, each with an ISO 8601 timestamp and details about the event.

# Required command line parameters:
## Parameter: **Action**
Value can be:
- Add
- Update
- Remove
 - Show

    ### Parameter: **Action** Value: *Add*
    Required parameters:
    - Type (string)
    - Category (string)
    - Name (string)

    Optional parameters:
    - Timestamp (string) - if this is specified then do not autogenerate a timestamp and use this as the timestamp for the event instead

        ####  Parameter: **Action** Value: *Add* - Parameter: **Type**
        Value can be:
        - Consumption
        - Measurement

            ##### Parameter: **Action** Value: *Add* - Parameter: **Type** Value: *Consumption* - Parameter: **Category**
            Value can be:
            - Substance
            - Food
            - Stack

                ##### Parameter: **Action** Value: *Add* - Parameter: **Type** Value: *Consumption* - Parameter: **Category** Value: *Substance*
                Required parameters:
                - Amount (float)
                - Unit (string)

                ##### Parameter: **Action** Value: *Add* - Parameter: **Type** Value: *Consumption* - Parameter: **Category** Value: *Food*
                Required parameters:
                - Amount (float)
                - Unit (string)

            ##### Parameter: **Action** Value: *Add* - Parameter: **Type** Value: *Measurement*
            Required parameters:
            - Value (float)
            - Unit (string)

                ##### Parameter: **Action** Value: *Add* - Parameter: **Type** Value: *Measurement* - Parameter: **Category**
                Value can be:
                - Vitals

    ### Parameter: **Action** Value: *Update*
    Required parameters:
    - Timestamp (string): needs to match a log entry's timestamp

    This needs to use the same code path as the Add Action after finding the log entry to benefit from it's validation of required parameters.

    Parameter specification is that one or more of the parameters required for Add are required and the unspecified ones are taken from the existing log entry. The purpose of this action is to retrieve the log entry with the specified Timestamp and update one or more of it's details.
    
    Note: The user's locale may differ from the one in ISO 8806 - please account for that. UTC ticks should be compared to fix it.

    ### Parameter: **Action** Value: *Remove*
    Required parameters:
    - Timestamp (string): needs to match a log entry's timestamp

    This action retrieves the log entry with the specified Timestamp and removes it from the array of entries. 
    Note: The user's locale may differ from the one in ISO 8806 - please account for that. UTC ticks should be compared to fix it.

    ### Parameter: **Action** Value: *Show*
    Shows log entries.

    Behavior:
    - If no parameters are provided: show today's entries (based on local date).
    - If `--timestamp` is provided:
        - If ISO 8601 roundtrip ("o"): match the entry with that exact stored Timestamp string.
        - Otherwise: interpret the timestamp as local time and match by instant (UTC ticks equality).
    - If `--date <date>` is provided: list entries on that local date.
    - If `--start <date>` and `--end <date>` are provided: list entries within that inclusive local date range.
    - If more than one of `--timestamp`, `--date`, or `--start`/`--end` are provided: error.
    - If only one of `--start` or `--end` is provided: error.

    Optional flags:
    - `--total`: instead of listing raw entries, print per-day totals for Consumption entries only. Totals are grouped by Category+Name+Unit and displayed using the same line format as entries, but with date-only (no time). When used with `--timestamp`, the timestamp is treated as selecting that local day.

    Output format:
    - One entry per line: `yyyy-MM-dd HH:mm:ss  Type/Category  Name  [Value Unit]`
    - Timestamps are displayed in local time without timezone offset.
    - Sorted ascending by time.