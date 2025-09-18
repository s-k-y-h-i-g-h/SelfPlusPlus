PowerShell script which lets the user log health and biohacking related things.

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
                - Amount (string)

            ##### Parameter: **Action** Value: *Add* - Parameter: **Type** Value: *Measurement*
            Required parameters:
            - Value (float)
            - Unit (string)

                ##### Parameter: **Action** Value: *Add* - Parameter: **Type** Value: *Measurement* - Parameter: **Category**
                Value can be:
                - Vitals