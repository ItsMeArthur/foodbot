using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoodBot.Constants;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

namespace FoodBot.Dialogs
{
    [Serializable]
    [LuisModel("a58f867c-152e-4628-b137-203840652eca", "c5e22a032add49e191a42154dd5ff845")]
    public class RootDialog : LuisDialog<object>
    {
        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Me desculpe. Não entendi. :/");
            context.Wait(MessageReceived);
        }

        [LuisIntent("PedirComida")]
        public async Task IntentOrderFood(IDialogContext context, LuisResult result)
        {
            // Se tem menos de seis entidades, está faltando alguma
            if (result.Entities.Where(e => e.Type != "dados_entrega").Count() < 5)
            {
                HandleMissingEntities(context, result);
            }
            // Se não faltou nenhuma entidade, ou obtemos as entidades faltantes, imprime o resultado.
            else
            {
                string food = result.Entities.FirstOrDefault(e => e.Type == ModelEntities.Food).Entity.ToUpper();
                string neighborhood = result.Entities.FirstOrDefault(e => e.Type == ModelEntities.Neighborhood).Entity.ToUpper();
                string number = result.Entities.FirstOrDefault(e => e.Type == ModelEntities.Number).Entity.ToUpper();
                string quantity = result.Entities.FirstOrDefault(e => e.Type == ModelEntities.Quantity).Entity.ToUpper();
                string street = result.Entities.FirstOrDefault(e => e.Type == ModelEntities.Street).Entity.ToUpper();

                await context.PostAsync($"Será entregue um prato {food} com quantidade {quantity} no endereço {street}, número {number}, bairro {neighborhood}.");
                context.Wait(MessageReceived);
            }
        }

        private void HandleMissingEntities(IDialogContext context, LuisResult result)
        {
            // Armazena o LuisResult no contexto pra usar mais adiante
            context.UserData.SetValue(UserDataKeys.LuisResult, result);

            
            if (!result.Entities.Any(e => e.Type == ModelEntities.Quantity))
            {
                context.UserData.SetValue(UserDataKeys.EntityToGet, ModelEntities.Quantity);
                string msg = "Preciso saber a quantidade de itens que você deseja. Por favor informe abaixo.";
                GetMissingEntityFromUserAsync(context, result, msg);
            }
            else if (!result.Entities.Any(e => e.Type == ModelEntities.Street))
            {
                context.UserData.SetValue(UserDataKeys.EntityToGet, ModelEntities.Street);
                string msg = "Qual a rua da entrega?";
                GetMissingEntityFromUserAsync(context, result, msg);
            }
            else if (!result.Entities.Any(e => e.Type == ModelEntities.Food))
            {
                context.UserData.SetValue(UserDataKeys.EntityToGet, ModelEntities.Food);
                string msg = "Qual o prato você deseja pedir?";
                GetMissingEntityFromUserAsync(context, result, msg);
            }
            else if (!result.Entities.Any(e => e.Type == ModelEntities.Number))
            {
                context.UserData.SetValue(UserDataKeys.EntityToGet, ModelEntities.Number);
                string msg = "Agora por favor informe o número da sua casa ou apartamento.";
                GetMissingEntityFromUserAsync(context, result, msg);
            }
            else if (!result.Entities.Any(e => e.Type == ModelEntities.Neighborhood))
            {
                context.UserData.SetValue(UserDataKeys.EntityToGet, ModelEntities.Neighborhood);
                string msg = "Qual o seu bairro?";
                GetMissingEntityFromUserAsync(context, result, msg);
            }
        }

        private void GetMissingEntityFromUserAsync(IDialogContext context, LuisResult result, string msg)
        {

            PromptDialog.Text(context, AfterGetMissingEntityFromUserAsync, msg);
        }

        private async Task AfterGetMissingEntityFromUserAsync(IDialogContext context, IAwaitable<string> result)
        {
            string userInput = await result;

            // Vamos acrescentar a entidade faltante no objeto de LuisResult, restaura ele aqui
            LuisResult luisResult = context.UserData.GetValue<LuisResult>(UserDataKeys.LuisResult);

            // Também precisamos saber o tipo da entidade a ser obtida
            string entityToGet = context.UserData.GetValue<string>(UserDataKeys.EntityToGet);

            // Criamos a entidade com base no input do usuário, só vamos adicionar aqui o que interessa
            EntityRecommendation entityToAdd = new EntityRecommendation
            {
                Type = entityToGet,
                Entity = userInput
            };

            // Adicionamos na coleção de entidades do LUIS
            var entities = new List<EntityRecommendation>(luisResult.Entities);
            entities.Add(entityToAdd);
            luisResult.Entities = entities;

            // E chamamos novamente o método IntentOrderFood, onde o LuisResult será revalidado
            await IntentOrderFood(context, luisResult);            
        }
    }
}