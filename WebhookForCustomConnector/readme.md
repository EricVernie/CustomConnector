
# Développer un connecteur personnalisé pour Power Automate et Azure Logic App à base de déclencheurs (Triggers)

Cet article assume que vous connaissiez déjà les basiques de la création d'un connecteur personnalisé pour Logic App ou Power Automate.

Je ne vais donc pas y revenir, mais si tel n'est pas le cas, je vous laisse le soin d’aller voir la [documentation en ligne](https://docs.microsoft.com/fr-fr/connectors/connectors).

Néanmoins, pour résumé, un connecteur

_C'est un proxy ou un wrapper autour d’une API qui permet au service sous-jacent de communiquer avec Microsoft Power Automate, Microsoft Power Apps et Azure Logic Apps. Il offre aux utilisateurs un moyen de se connecter à leurs comptes et de tirer parti d’un ensemble d’actions et de déclencheurs prédéfinis pour créer leurs applications et leurs workflows.

_Chaque connecteur offre un ensemble d’opérations classées comme « Actions » et « Déclencheurs ». Une fois que vous vous êtes connecté au service sous-jacent, ces opérations peuvent être facilement exploitées dans vos applications et vos workflows._

|Type d'opération| Commentaires|
| :------------- | :---------- |
|**Action**| _Par exemple, vous utiliseriez une action pour rechercher, écrire, mettre à jour ou supprimer des données dans une base de données._|
|**Déclencheurs** _Polling_| _Ces déclencheurs appellent votre service selon une fréquence spécifiée pour vérifier l’existence de nouvelles données. Lorsque de nouvelles données sont disponibles, cela entraîne une nouvelle exécution de votre instance de workflow avec les données en entrée_|
|**Déclencheurs** _Webhook| _Ces déclencheurs écoutent les données sur un point de terminaison, c'est-à-dire qu'ils attendent qu'un événement se produise. L'occurrence de cet événement provoque une nouvelle exécution de votre instance de workflow._  |

Mon idée ici est de vous montrer comment préparer les éléments nécessaires afin de pouvoir créer un connecteur à base de **déclencheur de type Webhook**. 

Si vous préférez suivre un didacticiel, je vous encourage à suivre celui en ligne [Utiliser un webhook en tant que déclencheur pour Azure Logic Apps et Power Automate](https://docs.microsoft.com/fr-fr/connectors/custom-connectors/create-webhook-trigger)

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

- Ils vous permettront de vous intégrer à moindre frais et facilement à la plate-forme Office 365, Azure, Dynamics 365, et autres SalesForce, Twitter, etc.. [connecteurs](https://emea.flow.microsoft.com/fr-fr/connectors/?filter=&category=all), et ceci que cela soit dans le cloud ou on-premise.

- Pas besoin de développer une solution d’intégration pour chaque système, les connecteurs disponibles sont fait pour ça.

- Vous permettez à vos clients non-développeurs de créer facilement leurs propres workflows d’intégration à l’aide de Power Automate.

## Logique d'un déclencheur webhook dans Logic App et Power Automate

1. Lors de l'enregistrement d'un Workflow, Logic App/Power Automate s'abonne au déclencheur, en lui passant une url de rappel dans le corps du message  
    |||
    | :------------- | :---------- |
    |Url|https://prod-00.francecentral.logic.azure.com/workflows/0182d35837c1417d859da07f8752bc9d/triggers/Lorsque_qu'un_nouveau_produit_arrive_dans_le_magasin....|
2. Le déclencheur sauvegarde dans son système cette url de rappel pour une utilisation future

3. Le déclencheur **doit impérativement retourner** à Logic App/Power Automate, **une url de suppression de l'abonnement**

4. Lorsqu'un évènement se passe, par exemple l'arrivée d'un produit en magasin, votre système initie le workflow à l'aide de l'url de rappel.

## Définition OpenAPI

L'intégration d'un connecteur personnalisé avec Logic App et Power Automate se fait par l'intermédiaire d'un fichier au format json, qui suit la spécification OpenAPI **version 2.0**, plus connue sous le nom de spécification [Swagger](https://swagger.io) , standard qui permet de définir les interfaces RestFull.

>**Note:** Logic App et Power Automate, ne supporte pas encore la version 3.0

Dans les lignes qui suivent nous allons donc voir comment :

- Créer le code du Webhook qui va gérer les abonnements
- Créer le fichier de définition OpenAPI

### Définition d'un déclencheur de type Webhook

[Microsoft a étendu la définition OpenAPI](https://docs.microsoft.com/fr-fr/connectors/custom-connectors/openapi-extensions) pour ses propres besoins, afin de pouvoir l'intégrer à Logic App et à Power Automate.

Pour définir un déclencheur de type webhook, il faut rajouter la propriété **"x-ms-trigger": "single"** dans la définition, afin d'indiquer à Logic App et Power Automate d'afficher l'opération en tant que déclencheur dans l'éditeur de connecteur personnalisé, comme illustré sur la figure suivante :
>Note : Ne pas mettre la propriété **"x-ms-trigger"** défini l'opération comme étant une **Action**.

![DEFINITION](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/Definition.png)

```json
"/event/instore": {
      "x-ms-notification-content": {
        "description": "Arrivée de nouveaux produits",
        "schema": {
          "$ref": "#/definitions/InStore"
        }
      },
      "post": {
        "description": "Lorsque qu'un nouveau produit arrive dans le magasin (version d'évaluation)",
        "summary": "Lorsque qu'un nouveau produit arrive dans le magasin (version d'évaluation)",
        "operationId": "NewInstoreProduct",
        "x-ms-trigger": "single",
        "parameters": [
          {
             "in": "body",
            "name": "Callback",
            "schema": {
              "$ref": "#/definitions/Callback"
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
....
 "definitions": {
    "Callback": {
      "type": "object",
      "required": [ "Url" ],
      "properties": {
        "Url": {
          "x-ms-notification-url": true,
          "x-ms-visibility": "internal",
          "required": [ "Url" ],
          "description": "URL de rappel",
          "title": "URL de rappel",
          "type": "string"
        }
      }
    },
    "InStore": {
      "type": "object",
      "properties": {
        "storeName": {
          "type": "string"
        },
        "productName": {
          "type": "string"
        },
        "quantity": {
          "format": "int32",
          "type": "integer"
        }
      }
    },
    ...

```

 |Propriété | Définition|
 | :------------- | :---------- |
 |**x-ms-notification-content**| Schéma de la charge utile qui sera envoyé à Logic App/Power Automate, lors du déclenchement de l'événement. En d'autres termes lorsqu'un nouveau produit arrive en magasin, c'est le schéma contenant les informations sur le produit qui sera envoyé à l'url de notification, afin de déclencher le Workflow. Par exemple si votre système reçois {storeName": "Magasin 1,"productName": "RTX 3090","quantity": 500}, vous le sauvegarderez, non seulement dans votre système, mais vous le fournirez également dans le corps du message lors de l'appel à l'url de rappel du workflow|
 |description & summary| Chaines de caractères affichées dans l'interface de l'éditeur de Workflow [Conseils sur les chaînes de connecteur](https://docs.microsoft.com/fr-fr/connectors/custom-connectors/connector-string-guidance)|
 |operationId| Id de l'opération|
 |**x-ms-trigger**| Défini une opération déclencheur de type Webhook|
 |**parameters**| Cette propriété est importante car elle définie en entrée **"in":"body"**, c'est à dire dans le corps du message le paramètre nommé arbitrairement **Callback** qui inclura l'url de rappel fournie par Logic App/Power Automate|

 Tous le champs définis dans La définition du Webhook sont important et à ne pas omettre

 |Propriété | Définition|
 | :------------- | :---------- |
 |**"required":["Url"]** | Indique que le champ Url est requis. |
 |**"x-ms-notification-url":"true"**| Indique à Logic App/Power Automate de placer l'URL de rappel dans le champ **Url**.|
 |**"x-ms-visibility":"internal"**|Indique que le champ doit être masqué aux utilisateurs.|

Voici sa représentation en C#, ce n'est ni plus ni moins qu'une classe avec une propriété de type string

```CSharp
public class CallBack
{
    public string Url { get; set; }
}
```

Et voici la représentation C# de l'opération **NewInstoreProduct**

```CSharp
public IActionResult NewInstoreProduct([FromBody] CallBack callback)
{                
    Subscription subscription = AddSubscription(callback, TypeEvent.InStore, this.Request.Headers);
    string location = $"https://{this.Request.Host.Host}/event/remove/{subscription.Oid}/{subscription.Id}/";    
    return new CreatedResult(location, null);
}       
```

>Note: Cette méthode sera appelée par Logic App/Power Automate avec l'url de rappel contenu dans la propriété **CallBack.Url**.

### Suppression d'un abonnement

Lors de l'inscription de l'abonnement du Workflow Logic App/Power Automate la méthode **NewInstoreProduct** est invoquée, c'est à ce moment-là qu'il faut que cette méthode retourne dans l'entête **Location** l'url qu'appellera Logic App/Power Automate, afin que l'abonnement soit supprimé. Cette action se déclenche lorsque le connecteur personnalisé n'est plus utilisé ou que le workflow l'utilisant est supprimé.

Afin de retrouver le bon abonnement à supprimer, j'ai décidé de la constituer d'un identificateur **Oid** représentant l'utilisateur authentifié et de l'identificateur **Id** numéro de la souscription renvoyé par Logic App/Power Automate dans l'entête "x-ms-workflow-subscription-id" (Nous y reviendrons un peu plus tard lorsque j'aborderai la sécurité du connecteur)

Le format de cette url sera donc "/event/remove/{subscription.Oid}/{subscription.Id}/", mais bien évidement ce n'est pas figé et dépendra sans doute de votre propre logique.

La définition OpenAPI pour la suppression de l'abonnement, doit donc reprendre le format de cette url, comme illustré dans l'extrait suivant.

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

Les deux paramètres **oid** et **id** sont requis et doivent être placés dans l'url elle même **"in":"path"**.
Comme c'est une opération de type Action, nous ne souhaitons pas qu'elle soit visible dans l'éditeur de workflow pour les utilisateurs **"x-ms-visibility":"internal"**

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

>Note : C'est une représentation très naïve, car les abonnements sont placés en mémoire dans une simple liste. Il faudra sans doute penser à un système plus robuste et autonome, afin de permettre à votre API d'avoir accès aux URL de rappels. Mais cela suffit ici pour nos besoins de démonstrations.

### Déclencher un workflow

Pour déclencher un workflow, c'est tout compte fait assez simple, il suffira juste d'un POST sur l'url de rappel renvoyée par Logic App/Power, en n'oubliant pas de passer le contenu du corps du message, comme illustré dans le code c# suivant.

```CSharp
 [HttpPost, Route("/fire/instore")]
 [AllowAnonymous]
 public async Task<IActionResult> FireInstore([FromBody] InStore inStore)
 {
     _inStores.Add(inStore);
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
             _logger.LogError(ex.Message);
         }
     }
     return Accepted($"Il y a {inStoreSubscriptions.Count} abonnement(s) au connecteur");
 }
```

### Création du connecteur personnalisé avec Power Automate

En l'état, il est possible de commencer à tester la création du connecteur personnalisé,
Nous allons le tester sur Power Automate, si vous n'avez pas d'abonnement vous pouvez obtenir un essai gratuit en suivant la procédure [ici](https://docs.microsoft.com/fr-fr/power-automate/sign-up-sign-in)

1. Récupérez le fichier [définition](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/OpenApiDefinition/OpenApiDefinition.json)

2. Connectez-vous au portail https://make.powerapps.com/

3. Dans le panneau gauche, sélectionnez "Données" | "Connecteurs Personnalisés"

4. En haut à droite sélectionnez "+ Nouveau connecteur personnalisé" | "Importer un fichier OpenAPI"

5. Dans la boîte de dialogue qui apparait, donnez un nom au connecteur puis | Importez le fichier que vous avez récupérez à l'étape 1

6. Vous noterez dans l'onglet "1.Général" que le champ Hôte n'est pas renseigné. Pour cela il vous faut publier le code de cet article. Néanmoins, vous pouvez continuer les étapes suivantes, sans vous en préoccuper outre mesure en indiquant dans le champ hôte toto.contoso.com , par contre à l'enregistrement du flux, rien ne se passera bien évidement. sinon suivez les sous-étapes suivantes : 

    1. [Abonnement Azure Gratuit](https://azure.microsoft.com/fr-fr/free/)

    2. [Téléchargement gratuit de visual Studio](https://visualstudio.microsoft.com/fr/vs/)

    3. Cloner le code

    4. [Publiez l'application sur Azure avec Visual Studio 2019](https://docs.microsoft.com/fr-fr/visualstudio/deployment/quickstart-deploy-to-azure?view=vs-2019)

    5. Ajoutez le champ Hôte du style [NOM DE L'APPLICATION].azurewebsites.com.

7. Allez ensuite dans l'onglet 3. Définition afin de vérifier qu'aucune erreur n'est survenue. Vous noterez à ce stade qu'aucun Déclencheur n'est disponible. Ceci peut être déroutant, mais ils sont bien présent. Vous pourrez le vérifier en éditant le swagger dans l'interface.

8. Sélectionnez "Créer le connecteur". Si tout se passe bien, le connecteur est créé.

9. Nous allons maintenant créer un Flux en sélectionnant dans le panneau Gauche "Flux"

10. Puis en haut de l'écran "+ Nouveau Flux" | "Flux de cloud automatisé"

11. Donnez un nom au flux, puis activez le bouton "Ignorer", afin de créer un flux vierge.

12. Sélectionnez l'onglet "Personnalisé" | puis sélectionnez le connecteur personnalisé que vous venez de créer

13. Deux déclencheurs devrait apparaitre comme illustré sur la figure suivante :
![DECLENCHEUR](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/declencheurs.png)

14. Sélectionnez "lorsqu'un nouveau produit arrive dans le magasin (version d'évaluation)", à ce stade comme aucune information de sécurité n'a été ajouté, le connexion se fait automatiquement.

15. Ajoutez une nouvelle étape de type "Notifications" | par exemple "Send me a mobile notification" ou tout autre à votre convenance.

16. Vous pouvez alors remplir, la zone de texte rapidement, en choisissant du contenu dynamique. Vous noterez la correspondance entre les champs du contenu dynamique et la définition/inStore du fichier de définition OpenAPI vu plus haut.
![DYNAMIQUE](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/contenudynamique.png)

### Sécurité du connecteur

Il est important que l'utilisateur puisse s'identifier avant de pouvoir utiliser le connecteur dans notre contexte, nous utiliserons Azure Active Directory, mais bien évidement c'est ouvert à d'autres fournisseurs d'identité.

#### Inscrire une application dans Azure Active Directory

Voici les différentes étapes à suivre :

1. A l'aide du portail https://aad.portal.azure.com/, sélectionnez "Azure Active Directory" | Inscription d'applications

2. "+ Nouvelle inscription" | Donnez un Nom | Cochez "Comptes dans un annuaire d'organisation (tout annuaire Azure AD - Multilocataire)

3. Bouton S'inscrire

4. Une fois l'application inscrite, sélectionnez "Vue d'ensemble" et copiez le GUID "ID d'application (client)"

5. Ensuite sélectionnez "Certificats & Secrets" | "+ Nouveau secret client" (copiez le secret pour une utilisation future)

6. Puis sélectionnez "API autorisées" | "+ Ajouter une autorisation" | "Microsoft Graph" | "Autorisations déléguées" | cochez "openid et profile"

7. Sélectionnez "Exposer une API" | "+ Ajouter une étendue" | Nom de l’étendue : impersonate | Qui peut accepter :" Administrateurs et Utilisateurs" | puis remplissez les autres champs obligatoires

8. Copiez l'étendue qui doit être de la forme api://[ID de l'application]/impersonate

    A ce stade vous devez avoir copié trois paramètres

    - ID d'application (client)

    - Le secret de l'application

    - l'étendue de l'application

9. Il nous reste encore un élément essentiel que nous n'avons pas encore renseigné, mais qui ne peut être que fourni que par l'éditeur de connecteur personnalisé Logic App/Power Automate, lorsqu'on renseigne les différents champs dans l'onglet sécurité c'est **l'url de redirection**.
Retournez sur le portail power automate et renseignez les champs dans l'onglet 2.Securité comme illustré sur la figure suivante :

    ![SECURITY](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/Securite2.png)

    | Champs       | Valeur         |
    | :------------- | :----------: |
    |Type Authentification| Oauth 2.0 |
    |  Fournisseur d'identité | Azure Active Directory  |
    |  Client id  | copiez ID d'application (client)|
    |  Client Secret   | copiez le secret de l'application|
    |  Resource Url | copiez ID d'application (client)* |
    |  Etendue  | copiez l'étendue de l'application|

    Une fois le connecteur enregistré, copiez **l'URL de redirection**, car nous allons finir l'enregistrement de notre application Azure Active Directory.

    >Note : Logic App retourne une URL du style : https://logic-apis-francecentral.consent.azure-apim.net/redirect, Power Automate retourne une URL du style : https://global.consent.azure-apim.net/redirect

10. Retournez sur le portail Azure Active Directory https://aad.portal.azure.com, sélectionnez l'application que vous venez d'inscrire

11. Sélectionnez "Authentification" | "+ Ajoutez une plateforme" | Application Web | Web | Copiez l'URI de redirection

    ![SECURITY](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/URI.png)

### Test de l'application

1. Editez le fichier [appsettings.json](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/appsettings.json) et copiez vos informations obtenues lors de l'enregistrement de l'application Azure Active Directory dans la section **AzureAd**.

```json
 "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "[VOTRE NOM DE DOMAINE ex demozonex.com]",
    "Audience": "[copiez ID d'application (client)]",
    "TenantId": "[copiez ID du tenant Azure Active Directory]",
    "ClientId": "[copiez ID d'application (client)]"
  },
```

2. [Publiez l'application sur Azure avec Visual Studio 2019](https://docs.microsoft.com/fr-fr/visualstudio/deployment/quickstart-deploy-to-azure?view=vs-2019)

3. Une fois l'application publiée, vous devez avoir un FQDN du style **[NOM DE L'APPLICATION].azurewebsites.net** qu'il faudra renseigner dans la propriété **host** du fichier de définition ou dans le champ Hôte de l'onglet 1.Général, lors de la création du connecteur

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

4. Créez un nouveau flux sur le portail power automate en prenant soin de supprimer toutes références aux connexions du connecteur personnalisé. Si tout fonctionne correctement vous devriez voir apparaître le bouton "Se connecter" comme illustrer sur la figure suivante.

    ![SECURITY](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/SecuriteFlux.png)

5. Une fois le flux crée, sélectionnez en haut à droit "Test" | "Manuellement" | "Enregistrer et tester"

6. Ensuite allez dans un navigateur, puis entrez l'url https://[NOM DE L'APPLICATION].azurewebsites.net/swagger

7. Sélectionnez la méthode POST /fire/instore comme illustré sur la figure suivante : 

    ![SWAGGER](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/swagger.png)

8. Et si tout a fonctionné, comme illustré sur la figure suivante.

    ![EXECUTIONFLUX](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/ExecutionFlux.png)

9. Si vous avez l'application Power Automate sur votre [mobile](https://flow.microsoft.com/en-us/mobile/download/), vous devriez recevoir la notification

    ![POWERAUTOMATE](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/PowerAutomate2.jpg)


### Conclusion

Les connecteurs personnalisés à base de déclencheurs, vont vous permettre de déverrouiller des scénarios, sans que vous soyez obligé d'investir lourdement dans le développement de solutions personnalisées.

Il y a quand même un peu de boulot et de code à écrire afin de créer le WebHook pour la gestion des abonnements, ainsi que d'intégrer vos évènements pour qu'il puissent déclencher les workflows. Mais franchement rien en comparaison si vous aviez à développer une solution personnalisée pour tous types de fournisseurs.

Appuyez-vous sur [l'écosystème des connecteurs personnalisés](https://docs.microsoft.com/en-us/connectors/connector-reference/) de Logic App/Power Automate, pour fournir à vos clients des solutions clés en main adaptées à leurs besoins.

### Bonus

Si vous avez installé l'application sur Azure, il est possible de s'abonner aux logs du Web Hook afin de pister les erreurs.

Pour ce faire vous pouvez utiliser l'utilitaire [az cli](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)

ensuite la commande az webapp log tail --name [NOM APPLICATION] -g [NOM DU RESSOURCE GROUPE] va vous permettre de vous connecter au log-streaming à partir de votre poste de travail

![LOGS](https://github.com/EricVernie/CustomConnector/blob/main/WebhookForCustomConnector/Doc/logs.png)


