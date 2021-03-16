
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

Dans ce tutorial, nous allons nous concentrer sur un connecteur de type Déclencheur WebHook.

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

![PowerAutomate](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/PowerAutomate.png)

## Définition OpenAPI ([Swagger](https://swagger.io/))
[Lien sur le fichier de définition](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/OpenApiDefinition/OpenApiV2ForConnector.json) de ce tutoriel.

L'intégration d'un connecteur personnalisé avec Logic App et Power Automate se fait par l'intermédiaire d'un fichier au format json, qui suit la spécification OpenAPI plus connue sous le nom de spécification [Swagger](https://swagger.io) **version 2.0**.
>**Note:** Logic App et Power Automate, ne supporte pas encore la version 3.0
C'est un standard qui permet de définir les interfaces RestFulf.

Par exemple le code CSharp suivant : 
```CSharp

public class Subscription
{
    public TypeEvent Event { get; set; }
    public string Id { get; set; }
    public string CallBackUrl { get; set; }
    public string Upn { get; set; }
    public string Oid { get; set; }
    public string Name { get; set; }
}

public class Webhook
{
    public string CallBackUrl { get; set; }
}
[HttpPost, Route("/event/neworder")]
public IActionResult NewEcommerceOrder([FromBody] Webhook body)
{
    //code omis pour plus de clarté
    return new CreatedResult(location, subscription);
}
```

Se matérialise en OpenAPI par :

```json
"/event/neworder": {      
      "post": {        
        "operationId": "NewEcommerceOrder",        
        "parameters": [
          {
            "in": "body",
            "name": "Webhook",
            "required": true,
            "schema": { "$ref": "#/definitions/Webhook" }
          }
        ],
        "responses": {
          "201": {
            "description": "Success",
            "schema": { "$ref": "#/definitions/Subscription" }
          }
        }
      }
    },
"definitions": {
    "TypeEvent": {
      "format": "int32",
      "enum": [1,2],
      "type": "integer"
    }, 
    "Subscription": {
      "type": "object",
      "properties": {
        "event": {
          "$ref": "#/definitions/TypeEvent"
        },
        "id": {"type": "string"},
        "callBackUrl": {"type": "string"},
        "upn": {"type": "string"},
        "oid": {"type": "string"},
        "name": {"type": "string"}
      }
    },
    "Webhook": {
      "type": "object",      
      "properties": {
        "callBackUrl": { "type": "string"}
        }
      }
    }
  }
```

Cet extrait de définition défini l'opération NewEcommerceOrder, et pour l'instant telle  quelle, elle sera considérée par Logic App et Power Automate comme un type action, et non pas comme un déclencheur.

C'est pourquoi [Microsoft a étendu la définition OpenAPI](https://docs.microsoft.com/fr-fr/connectors/custom-connectors/openapi-extensions) pour ses propres besoins, afin de pouvoir l'intégrer à Logic App et à Power Automate.

pour une intégration réussi il faudra rajouter à la définition de notre opération les propriétés suivantes, 
**_x-ms-notification-content_** et **_x-ms-trigger_** de la manière suivante

```json
 "/event/neworder": {
      "x-ms-notification-content": {
        "description": "Création d'une nouvelle commande"
      },
      "post": {
        "description": "Lorsqu'une nouvelle commande est créée (version d'évaluation)",
        "summary": "Lorsqu'une nouvelle commande est créée  (version d'évaluation)",
        "operationId": "NewEcommerceOrder",
        "x-ms-trigger": "single",

```

La propriété "x-ms-trigger": "single" va indiquer à Logic App et Power Automate d'afficher notre opération en tant que déclencheur dans l'éditeur de connecteur personnalisé.

![DEFINITIION](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/Definition.png)
> Plusieurs remarques :

