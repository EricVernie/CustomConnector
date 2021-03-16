
## Développer un connecteur personnalisé pour Power Automate et Azure Logic App à base de déclencheurs (Triggers).

Je ne reviendrais pas sur ce qu’est un connecteur, je vous laisse le soin d’aller voir la documentation [Connecteurs](https://docs.microsoft.com/fr-fr/connectors/connectors)

Pour résumé, un connecteur peut-être de type :

**Action**

_Par exemple, vous utiliseriez une action pour rechercher, écrire, mettre à jour ou supprimer des données dans une base de données._ 

**Déclencheurs**

Polling :
_Ces déclencheurs appellent votre service selon une fréquence spécifiée pour vérifier l’existence de nouvelles données. Lorsque de nouvelles données sont disponibles, cela entraîne une nouvelle exécution de votre instance de workflow avec les données en entrée_

Webhook :
_Ces déclencheurs écoutent les données sur un point de terminaison, c'est-à-dire qu'ils attendent qu'un événement se produise. L'occurrence de cet événement provoque une nouvelle exécution de votre instance de workflow._

Ici nous allons nous concentrer sur un connecteur de type Déclencheur WebHook.

Lorsqu’un évènement se passe sur votre plate-forme, le déclencheur en informera le connecteur qui pourra alors initier un workflow Logic App ou Power Automate.





Les avantages sont nombreux, du fait même qu’ils existent de nombreux autres connecteurs 
[Logic App](https://docs.microsoft.com/fr-fr/azure/connectors/apis-list#:~:text=Connectors%20provide%20quick%20access%20from%20Azure%20Logic%20Apps,the%20data%20that%20you%20create%20and%20already%20have.)
 et [Power Automate](https://emea.flow.microsoft.com/fr-fr/connectors/)

- Ils vous permettront de vous intégrer à moindre frais et facilement à la plate-forme Microsoft 365, à des systèmes tiers Dynamics, Sales Force, à de l’intelligence artificielle etc…, et ceci que cela soit dans le cloud ou on-premise.

- Pas besoin de développer une solution d’intégration pour chaque système les connecteurs disponibles sont fait pour ça.

- Vous permettez à vos clients non-développeurs de créer facilement leurs propres workflows d’intégration à l’aide de Power Automate.

Par exemple, imaginons le scénario suivant. Je suis directeur de secteur d’une grande épicerie en ligne, et j’aimerai pouvoir être notifié lorsqu’un magasin réceptionne de nouvelles marchandises. 

Bien évidemment, vous avez prévu le cas, et votre plate-forme le gère correctement et affiche les notifications. Mais en tant que directeur, je suis souvent sur les routes, pas toujours connecté à mon PC. 
En créant un connecteur personnalisé, vous allez ajouter de la souplesse à votre système, en permettant au directeur (ou à son IT) de créer un Workflow très simple, qui va dés réception d’une marchandise notifier l’application Mobile Power Automate, comme illustrer sur l’image suivante.

![InitCode](https://github.com/EricVernie/CloudNativeAppForAzureDev/blob/step1/docs/step1.png)
