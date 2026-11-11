# Local database scripts

Run from this folder with Windows authentication. Replace each `<password>` with a secret value (never commit it).

```
sqlcmd -S localhost -E -C -i 00-database.sql
sqlcmd -S localhost -E -C -i 01-schemas-and-users.sql -v CustomersAppPassword="<password>" WorkshopsAppPassword="<password>" FleetAppPassword="<password>" MaintenanceAppPassword="<password>" ServiceOrdersAppPassword="<password>" ImportsAppPassword="<password>" ReportingAppPassword="<password>" EventBusAppPassword="<password>"
```

Both scripts are idempotent. Tables are created by the EF Core migrations of each service with an administrative user.
