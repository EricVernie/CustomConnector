
# Développer un connecteur personnalisé pour Power Automate et Azure Logic App à base de déclencheurs (Triggers).

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

## Logique d'un déclencheur dans Logic App et Power Automate

1. Lors de l'enregistrement d'un Workflow, Logic App/Power Automate s'abonne au déclencheur en lui passant une url de rappel dans le corps du message

2. Le déclencheur sauvegarde dans son système cette url de rappel pour une utilisation future

3. Le déclencheur **doit impérativement retourner** à Logic App/Power Automate, **une url de suppression de l'abonnement**

4. Lorsqu'un évènement se passe, par exemple l'ajout d'une nouvelle commande, votre système initie le workflow à l'aide de l'url de rappel

L'intégration d'un connecteur personnalisé avec Logic App et Power Automate se fait par l'intermédiaire d'un fichier au format json, qui suit la spécification OpenAPI plus connue sous le nom de spécification [Swagger](https://swagger.io) **version 2.0**.
>**Note:** Logic App et Power Automate, ne supporte pas encore la version 3.0
C'est un standard qui permet de définir les interfaces RestFull.

Dans les lignes qui suivent nous allons donc voir comment créer, le code du Webhook qui va gérer les abonnements, le fichier de définition OpenAPI et les différentes implications entre ces deux composants.

### Définition d'un déclencheur

[Microsoft a étendu la définition OpenAPI](https://docs.microsoft.com/fr-fr/connectors/custom-connectors/openapi-extensions) pour ses propres besoins, afin de pouvoir l'intégrer à Logic App et à Power Automate.

Pour définir un déclencheur, il faut rajouter la propriété "x-ms-trigger": "single" qui va indiquer à Logic App et Power Automate d'afficher l'opération en tant que déclencheur dans l'éditeur de connecteur personnalisé, comme illustré sur la figure suivante :

![DEFINITIION](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/Definition.png)

```json
"/event/instore": {
      "x-ms-notification-content": {
        "description": "Arrivée de nouveaux produits"
      },
      "post": {
        "description": "Lorsque qu'un nouveau produit arrive dans le magasin (version d'évaluation)",
        "summary": "Lorsque qu'un nouveau produit arrive dans le magasin (version d'évaluation)",
        "operationId": "NewInstoreProduct",
        "x-ms-trigger": "single",
        "parameters": [
          {
            "in": "body",
            "name": "Webhook",
            "schema": {
              "$ref": "#/definitions/Webhook"
            }
          }
        ],
        "responses": {
          "201": {
            "description": "Success"
          }
        }
      }
    },

```



Par exemple le code CSharp suivant :

```CSharp
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

Cet extrait défini l'opération _NewEcommerceOrder_ mais pour l'instant telle  quelle, elle sera considérée par Logic App et Power Automate comme un type Action, et non pas comme un déclencheur.



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






[Lien sur le fichier de définition](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/OpenApiDefinition/OpenApiV2ForConnector.json) de ce tutoriel.
