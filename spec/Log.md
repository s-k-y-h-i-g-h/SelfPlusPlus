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
            - Stack

                ##### Parameter: **Action** Value: *Add* - Parameter: **Type** Value: *Consumption* - Parameter: **Category** Value: *Substance*
                Required parameters:
                - Value (float)
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