
# Développer un connecteur personnalisé pour Power Automate et Azure Logic App à base de déclencheurs (Triggers)

Cet article assume que vous connaissiez déjà les basiques de la création d'un connecteur personnalisé pour Logic App ou Power Automate.

Je ne vais donc pas y revenir, mais si tel n'est pas le cas, je vous laisse le soin d’aller voir la [documentation en ligne](https://docs.microsoft.com/fr-fr/connectors/connectors).

Néanmoins, pour résumé, un connecteur peut-être de type :

**Action**

_Par exemple, vous utiliseriez une action pour rechercher, écrire, mettre à jour ou supprimer des données dans une base de données._

**Déclencheurs**

*Polling* :
_Ces déclencheurs appellent votre service selon une fréquence spécifiée pour vérifier l’existence de nouvelles données. Lorsque de nouvelles données sont disponibles, cela entraîne une nouvelle exécution de votre instance de workflow avec les données en entrée_

*Webhook* :
_Ces déclencheurs écoutent les données sur un point de terminaison, c'est-à-dire qu'ils attendent qu'un événement se produise. L'occurrence de cet événement provoque une nouvelle exécution de votre instance de workflow._

Mon idée ici est de vous montrer comment préparer les élèments necessaires afin de pouvoir créer un connecteur à base de **déclencheur de type Webhook**

## Pourquoi développer un connecteur personnalisé ?

Imaginons le scénario très simple suivant. Je suis directeur d’une grande épicerie en ligne, j'utilise votre plate-forme d'ecommerce, qui permet à mes fournisseurs d'ajouter de nouveaux produits dans mon catalogue.

1. J’aimerai pouvoir être notifié, lorsqu'un fournisseur demande l'ajout d'un produit dans le catalogue, à des fins d'approbation de la demande

2. Lorsqu'un magasin réceptionne de nouvelles marchandises.

Bien évidemment, vous avez prévu le cas, et votre plate-forme le gère correctement et affiche les notifications. Mais en tant que directeur, je suis souvent sur les routes, pas toujours connecté à mon PC. En créant un connecteur personnalisé, vous allez ajouter de la souplesse à votre système, en permettant au directeur (ou à son IT) de créer un Workflow très simple, qui va par exemple, dés réception d’une marchandise notifier l’application Mobile Power Automate, comme illustrer sur l’image suivante.

![PowerAutomate](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/PowerAutomate.png)

Lorsqu’un évènement se passe sur votre plate-forme, le déclencheur en informera le connecteur qui pourra alors initier un workflow Logic App ou Power Automate.

Les avantages sont nombreux, du fait même qu’ils existent de nombreux autres connecteurs
[Logic App](https://docs.microsoft.com/fr-fr/azure/connectors/apis-list#:~:text=Connectors%20provide%20quick%20access%20from%20Azure%20Logic%20Apps,the%20data%20that%20you%20create%20and%20already%20have.)
 et [Power Automate](https://emea.flow.microsoft.com/fr-fr/connectors/)

- Ils vous permettront de vous intégrer à moindre frais et facilement à la plate-forme Microsoft 365, à des systèmes tiers Dynamics, Sales Force, à de l’intelligence artificielle etc…, et ceci que cela soit dans le cloud ou on-premise.

- Pas besoin de développer une solution d’intégration pour chaque système, les connecteurs disponibles sont fait pour ça.

- Vous permettez à vos clients non-développeurs de créer facilement leurs propres workflows d’intégration à l’aide de Power Automate.


## Logique d'un déclencheur dans Logic App et Power Automate

1. Lors de l'enregistrement d'un Workflow, Logic App/Power Automate s'abonne au déclencheur en lui passant une url de rappel dans le corps du message

2. Le déclencheur sauvegarde dans son système cette url de rappel pour une utilisation future

3. Le déclencheur **doit impérativement retourner** à Logic App/Power Automate, **une url de suppression de l'abonnement**

4. Lorsqu'un évènement se passe, par exemple l'ajout d'une nouvelle commande, votre système initie le workflow à l'aide de l'url de rappel

L'intégration d'un connecteur personnalisé avec Logic App et Power Automate se fait par l'intermédiaire d'un fichier au format json, qui suit la spécification OpenAPI plus connue sous le nom de spécification [Swagger](https://swagger.io) **version 2.0**, standard qui permet de définir les interfaces RestFull.

>**Note:** Logic App et Power Automate, ne supporte pas encore la version 3.0


Dans les lignes qui suivent nous allons donc voir comment créer, le code du Webhook qui va gérer les abonnements, le fichier de définition OpenAPI et les différentes implications entre ces deux composants.

### Définition d'un déclencheur

[Microsoft a étendu la définition OpenAPI](https://docs.microsoft.com/fr-fr/connectors/custom-connectors/openapi-extensions) pour ses propres besoins, afin de pouvoir l'intégrer à Logic App et à Power Automate.

Pour définir un déclencheur, il faut rajouter la propriété **"x-ms-trigger": "single"** qui va indiquer à Logic App et Power Automate d'afficher l'opération en tant que déclencheur dans l'éditeur de connecteur personnalisé, comme illustré sur la figure suivante.
Ne pas la mettre défini l'opération comme étant une Action.

![DEFINITION](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/Definition.png)

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

Outre les propriétés description et summary, la propriété **"operationId":"NewInstoreProduct"** qui définie le nom du déclencheur, la propriété **parameters** est trés importante, car elle définie en entrée **"in":"body"**, c'est à dire dans le corps du message le paramètre nommé arbitrairement **Webhook** qui inclura l'url de rappel fournie par Logic App/Power Automate, dont voici sa représentation.

```json
"Webhook": {
      "type": "object",
      "required": [ "callBackUrl" ],
      "properties": {
        "callBackUrl": {
          "x-ms-notification-url": true,
          "x-ms-visibility": "internal",
          "required": [ "callBackUrl" ],
          "description": "URL de rappel",
          "title": "URL de rappel",
          "type": "string"
        }
      }
    }
```

**"required":["callBackUrl"]** indique que le champ callBackUrl est requis.

**"x-ms-notification-url":"true"** va indiquer à Logic App/Power Automate de placer l'URL de rappel dans le champ callBackUrl.

**"x-ms-visibility":"internal"** indique que le champ doit être masqué aux utilisateurs.

Voici sa représentation en C#, ce n'est ni plus ni moins qu'une classe avec une propriété de type string

```CSharp
public class Webhook
{
    public string CallBackUrl { get; set; }
}
```

Et voici la réprésentation C# de l'opération **NewInstoreProduct**

```CSharp
[HttpPost, Route("/event/instore")]
public IActionResult NewInstoreProduct([FromBody] Webhook body)
{
  Subscription subscription = AddSubscription(body, TypeEvent.InStore, this.Request.Headers);
  string location = $"https://{this.Request.Host.Host}/event/remove/{subscription.Oid}/{subscription.Id}/"; 
  return new CreatedResult(location, null);
}
```

Cette méthode sera appelée par Logic App/Power Automate avec l'url de rappel contenu dans la propriété **WebHook.CallBackUrl**.

Nous reviendrons plus en détails plus tard sur la méthode **AddsSubscription** qui sauvegarde entre autre l'url de rappel, mais notez que cette méthode retourne dans l'entête **Location** l'url qu'appelera Logic App/Power Automate, lorsque le connecteur personnalisé ou le workflow l'utilisant sera supprimé.

Le format de cette url "https://{this.Request.Host.Host}/event/remove/{subscription.Oid}/{subscription.Id}/" est arbitraire, elle dépendra uniquement de votre logique.

Ici j'ai décidé de la constituer du champ **Oid** qui représente un numéro d'identification de l'utilisateur authentifié et du champ **Id** numéro de la souscription renvoyé par Logic App/Power Automate que nous verrons un peu plus tard lorsque j'aborderai la sécurité du connecteur.

La définition OpenAPI doit donc inclure impérative une définition pour la suppression de l'abonnement, et ceci en accord avec le format de cette url.

```json
 "/event/remove/{oid}/{id}": {
      "delete": {
        "description": "Supprimer l'abonnement",
        "summary": "Supprimer l'abonnement",
        "operationId": "RemoveSubscription",
        "x-ms-visibility": "internal",
        "parameters": [
          {
            "in": "path",
            "name": "oid",
            "required": true,
            "type": "string"
          },
          {
            "name": "id",
            "in": "path",
            "description": "Identification de l'abonnement",
            "required": true,
            "type": "string"
          }
        ],
        "responses": {
          "200": {
            "description": "Success"
          }
        }
      }
    }

```

Ici nous définissons deux paramètres **oid** et **id** requis qui seront placés dans l'url elle même **"in":"path"**.

Vous noterez que c'est une opération de type Action, il est donc impératif qu'elle ne soit pas visible pour les utilisateurs **"x-ms-visibility":"internal"**

La représentation C# est la suivante :

```CSharp
 [HttpDelete, Route("/event/remove/{oid}/{id}")]
 public IActionResult RemoveSubscription(string oid,string id)
 {
    var itemToRemove = _subscriptions.Single(r => r.Id == id && r.Oid == oid);
    _subscriptions.Remove(itemToRemove);           
    return Ok();
}
```

>Note : C'est une représentation trés naive, car les abonnements sont placés en mémoire dans une simple liste. Il faudra sans doute penser à un système plus robuste et autonome. Mais cela suffit ici pour nos besoins de démonstrations.

Si vous souhaitez voir tout de suite ce que cela donne avec l'éditeur de connecteur personnalisé, voici le [Lien sur le fichier de définition](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/OpenApiDefinition/OpenApiV2ForConnector.json), puis en suivant les instructions [ici](https://docs.microsoft.com/fr-fr/connectors/custom-connectors/define-openapi-definition)

### Securité du connecteur

Il est important que l'utilisateur puisse s'identifier avant de pouvoir utiliser le connecteur. Nous utiliserons dans notre cas Azure Active Directory

#### Inscrire une application dans Azure Active Directory
Voici les différentes étapes à suivre :

1. A l'aide du portail https://aad.portal.azure.com/, selectionnez "Azure Active Directory" | Inscription d'applications

2. "+ Nouvelle inscription" | Donnez un Nom | Cochez "Comptes dans un annuaire d'organisation (tout annuaire Azure AD - Multilocataire)

3. Bouton S'inscrire

4. Une fois l'application inscrite, selectionnez "Vue d'ensemble"  et copiez le GUID "ID d'application (client)"

5. Ensuite selectionnez "Certificats & Secrets" | "+ Nouveau secret client" (copiez le secret pour une utilisation future)

6. Puis selectionnez "API autorisées" | "+ Ajouter une autorisation" | "Microsoft Graph" | "Autorisations déléguées" | cochez "openid et profile"

7. Selectionnez "Exposer une API" | "+ Ajouter une étendue" | Nom de l'étendue: impersonate | Qui peut accepter :" Administrateurs et Utilisateurs" | puis remplissez les autres champs obligatoires

8. Copiez l'étendue qui doit être de la forme api://[ID de l'application]/impersonate

    A ce stade vous devez avoir copié trois paramètres

    - ID d'application (client)

    - Le secret de l'application

    - l'étendue de l'application

9. Il nous reste encore un élèment essentiel que nous n'avons pas encore renseigné, mais qui ne peut être que fourni que par l'éditeur de connecteur personnalisé Logic App/Power Automate, lorsqu'on renseigne les différents champs dans l'onglet sécurité c'est **l'url de redirection**. Comme illustré sur la figure suivante :

    ![SECURITY](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/Securite2.png)

    | Champs       | Valeur         |
    | :------------- | :----------: |
    |Type Authentification| Oauth 2.0 |
    |  Fournisseur d'identité | Azure Active Directory  |
    |  Client id  | copiez ID d'application (client)|
    |  Client Secret   | copiez le secret de l'application|
    |  Etendue  | copiez l'étendue de l'application|

    Une fois le connecteur enregistré, copiez **URL de redirection**, car nous allons finir l'enregistrement de notre application Azure Active Directory.

    >Note : Logic App retourne une URL du style : https://logic-apis-francecentral.consent.azure-apim.net/redirect, Power Automate retourne une URL du style : https://global.consent.azure-apim.net/redirect

10. Retournez sur le portail Azure Active Directory https://aad.portal.azure.com, selectionnez l'application que vous venez d'inscrire

11. Selectionnez "Authentification" | "+ Ajoutez une plateforme" | Application Web | Web | Copiez l'URI de redirection

    ![SECURITY](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/URI.png)

12. Editez le fichier [appsettings.json](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/appsettings.json) et copiez vos informations de l'application Azure Active Directory dans la section **AzureAd**

```json
 "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "[VOTRE NOM DE DOMAINE ex demozonex.com]",
    "Audience": "[copiez ID d'application (client)]",
    "TenantId": "[copiez ID du tenant Azure Active Directory]",
    "ClientId": "[copiez ID d'application (client)]"
  },
```

Maintenanons que nous avons mis en place la sécurité de notre connecteur revenons maintenant sur la méthode **AddSubscription**.

Cette méthode nous permet de sauvegarder, non seulement le numéro de l'abonnement du workflow **x-ms-workflow-subscription-id**, mais également le numéro d'identification Azure Active Directory de l'utilisateur connecté.
Ceci va nous permettre de construire notre url de suppression sous la forme /event/remove/{subscription.Oid}/{subscription.Id}

```CSharp
 private Subscription AddSubscription(Webhook body,TypeEvent typeEvent, IHeaderDictionary headers)
 {

     Subscription subscription = null;
     // Récupère le numéro d'abonnement du workflow afin de le stocker pour le retrouver
     // pour suppression.
     var SubId = headers.Where(x => x.Key == "x-ms-workflow-subscription-id")
                        .Select(x => x)
                        .First();
     // L'entête doit forcement contenir un jeton d'accès sous la forme
     // Authorization:Bearer eyJ0eXAiOiJKV1QiLCJhbGciOi.....
     var BearerToken = headers.Where(x => x.Key == "Authorization")
                    .Select(x => x)
                    .First();                       
     string Token = BearerToken.Value;

     // Supprime le mot Bearer + l'espace entre le mot et le jeton
     Token = Token.Remove(0, 7);

     var Handler = new JwtSecurityTokenHandler();
     var AccessToken = Handler.ReadJwtToken(Token);

     subscription = new Subscription
     {
         Event = typeEvent,
         CallBackUrl = body.CallBackUrl,
         Id = SubId.Value,
         Name = GetClaimValue(AccessToken, "name"),
         Upn = GetClaimValue(AccessToken, "upn"),
         Oid = GetClaimValue(AccessToken, "oid")
     };
     
     // Le stockage des abonnements se fait en mémoire
     // Bien évidement il faudra utiliser un système plus robuste.
     _subscriptions.Add(subscription);
     return subscription;
 }
```

13. [Publiez l'application sur Azure avec Visual Stduio 2019](https://docs.microsoft.com/fr-fr/visualstudio/deployment/quickstart-deploy-to-azure?view=vs-2019)

14. Une fois l'application publiée, vous devez avoir un FQDN du style **[NON DE L'APPLICATION].azurewebsites.net** qu'il faudra renseigner dans la propriété **host** du fichier de définition.

```json
{
  "swagger": "2.0",
  "info": {
    "title": "Exemple de connecteur personnalisé",
    "version": "v1"
  },
  "host": "webhookforcustomconnector.azurewebsites.net",
  "basePath": "/",
  "schemes": [ "https" ],
```

Enfin pour invoquer nos différents workflow un simple POST sur les URL de rappels envoyées par Logic App/Power Automate lorsque l'évènement se produit.

```CSharp
[HttpPost, Route("/fire/instore")]
[AllowAnonymous]
public async Task<IActionResult> FireInstore([FromBody] InStore inStore)
{
    var inStoreSubscriptions = _subscriptions
                    .Where(s => s.Event == TypeEvent.InStore)
                    .Select(s => s).ToList();
    var client = _clientFactory.CreateClient();
    foreach (var instore in inStoreSubscriptions)
    {
        string jsonData = JsonConvert.SerializeObject(inStore);
        StringContent stringContent = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
        try
        {
            await client.PostAsync(instore.CallBackUrl, stringContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    return Accepted($"Il y a {inStoreSubscriptions.Count} abonnement(s) au connecteur");
}
```

>Note: A des fins de démonstration et simplicité j'ai choisi d'intégrer le déclenchement de l'évènement avec le code d'inscription des abonnements.

Les connecteurs personnalisés à base de déclencheurs, vont vous permettre de dévérouiller des scénarios, sans pour autant lourdement investir dans le développement de solutions personnalisées.
De s'intégrer facilement et rapidement aux systèmes d'informations de vos clients et de leur permettre de construire leur propre solution de workflow.
