Import-Module ConsoleMonster

# Open the diagram: Space plays the use case, Left and Right step, Tab switches use case, Esc quits.
Show-ConsoleDiagram -Path ./ad-replication.cfx

# One step as text, for a log or a ticket.
Show-ConsoleDiagram -Path ./ad-replication.cfx -Scenario sitelink -Step 4 -Width 120 -AsString
