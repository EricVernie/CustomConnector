using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using WebHookForCustomConnector.DataModel;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace WebHookForCustomConnector.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class StoreEventTrigger : ControllerBase
    {
        /// <summary>
        ///
        /// </summary>
        public static List<Subscription> _subscriptions = new List<Subscription>();

        public static OrderDetails _newOrders = new OrderDetails();


        private readonly IHttpClientFactory _clientFactory;
        private readonly TelemetryClient _telemetry;
        private readonly ILogger<StoreEventTrigger> _logger;
        private readonly IConfiguration _configuration;
        public StoreEventTrigger(IHttpClientFactory clientFactory,TelemetryClient telemetry, ILogger<StoreEventTrigger> logger, IConfiguration configuration)
        {
            _clientFactory = clientFactory;
            _logger = logger;
            _configuration = configuration;
        }

      

        /// <summary>
        /// Receive a subscription to a webhook.  
        /// </summary>
        /// <param name="Subscription">URL to get from Logic Apps or Power Automate- @listCallbackUrl()</param>
        /// <returns></returns>
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [HttpPost, Route("/event/neworder")]
        public IActionResult NewEcommerceOrder([FromBody] Webhook body)
        {
            _logger.LogInformation("Abonnement à l'évènement 'Lorsqu'une nouvelle commande est créée'");            
            Subscription subscription = AddSubscription(body,TypeEvent.NewOrder, this.Request.Headers);
            string location = $"https://{this.Request.Host.Host}/event/remove/{subscription.Oid}/{subscription.Id}";
            _logger.LogInformation($"Location: {location}");
            return new CreatedResult(location, null);
        }
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [HttpPost, Route("/event/instore")]
        public IActionResult NewInstoreProduct([FromBody] Webhook body)
        {
            _logger.LogInformation("Abonnement à l'évènement 'Arrivée de nouveaux produits' ");
            Subscription subscription = AddSubscription(body, TypeEvent.InStore, this.Request.Headers);
            string location = $"https://{this.Request.Host.Host}/event/remove/{subscription.Oid}/{subscription.Id}/";
            _logger.LogInformation($"Location: {location}");
            return new CreatedResult(location, null);
        }
      
        [HttpGet, Route("/list/neworders")]
        public List<Order> GetListNewOrders()
        {            
            return _newOrders.Orders;
        }
        /// <summary>
        /// Unsubscribe
        /// </summary>
        /// <param name="callbackUrl"></param>
        /// <returns></returns>
        [HttpDelete, Route("/event/remove/{oid}/{id}")]
        public IActionResult RemoveSubscription(string oid,string id)
        {
            _logger.LogInformation($"Suppression de l'abonnement : {id}");
            
            var itemToRemove = _subscriptions.Single(r => r.Id == id && r.Oid == oid);
            _subscriptions.Remove(itemToRemove);
            
            return Ok();
        }


        #region TOOLS
        private string GetClaimValue(JwtSecurityToken token, string claim)
        {
            return token.Claims.ToList()
                                     .Where(x => x.Type == claim)
                                     .Select(x => x)
                                     .First().Value;


        }
     
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
        #endregion
        #region FOR TEST ONLY

        [HttpGet]
        [AllowAnonymous]
        public List<Subscription> Get()
        {
            return _subscriptions;
        }


        /// <summary>
        /// To Test Fire all event for new Order  - do a GET to this API to fire all triggers subscribed
        /// </summary>
        /// <returns></returns>
        [HttpPost, Route("/fire/neworder")]        
        [AllowAnonymous]
        public async Task<IActionResult> FireNewOrder([FromBody] Order newOrder)
        {
            // Sauvegarde la nouvelle commande
            _newOrders.Orders.Add(newOrder);

            // Retrouve les abonnements à Logic App ou Power Automate
            // 
            var newOrderSubscriptions = _subscriptions
                            .Where(s => s.Event == TypeEvent.NewOrder)
                            .Select(s => s).ToList();


            var client = _clientFactory.CreateClient();
            foreach (var sub in newOrderSubscriptions)
            {
                string jsonData = JsonConvert.SerializeObject(newOrder);
                StringContent stringContent = new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json");
                try
                {
                    await client.PostAsync(sub.CallBackUrl, stringContent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
            return Accepted($"Il y a {newOrderSubscriptions.Count} abonnement(s) au connecteur");
        }
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
        #endregion
    }
}
