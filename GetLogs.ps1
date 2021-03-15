Param(
    [string] [Parameter(Mandatory=$false)] $rgname="training-logicapp-rg",
    [string] [Parameter(Mandatory=$false)] $botname="webhookforcustomconnector")

az webapp log tail --name $botname -g $rgname