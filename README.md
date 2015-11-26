# Hansoft-Jean-DependencyBehavior

##DependencyBehavior
This behavior updates two different custom columns, “Internal dependencies” and “External dependencies”. If a dependency is internal or external is defined by the ProgramTeams.xml file. Note that team projects should have the naming convention: “Team - xyz”.

Internal dependency is updated if more that one team have pending stories linked to a program feature. Once the stories are completed the team is removed from the dependency list. When there is only one team left the column is cleared to indicate that all dependencies have been resolved. The teams are listed in order of planned sprint completion order with the help of the custom column value set in “Planned sprint”.

External dependency is updated if any team not belonging to the program have linked any items to a milestones defined by the program. If there are multiple milestones tagged to a feature and at least one milestone is linked to an external team the team name will be listed in the External dependencies column.
