using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using AdaptiveCards;

namespace WaterfallBot.Dialogs
{
    public class UserDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfileClass> _UserProfileAccessor;
        public UserDialog(UserState userstate) : base(nameof(UserDialog))
        {
            _UserProfileAccessor = userstate.CreateProperty<UserProfileClass>("UserProfileClass");
            var waterfallstep = new WaterfallStep[]
            {
                GenderStepAsync,
                NameStepAsync,
                AgeStepAsync,
                VoterStepAsync,
                ConfirmStepAsync,
                SummaryStepAsync,
            };
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallstep));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new NumberPrompt<int>(nameof(NumberPrompt<int>), AgeValidatorAsync));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> GenderStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Select Your Gender "),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Male", "Female" }),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["gender"] = ((FoundChoice)stepContext.Result).Value;
            return await stepContext.PromptAsync(nameof(TextPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Enter Your Name ")
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> AgeStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["name"] = (string)stepContext.Result;
            return await stepContext.PromptAsync(nameof(NumberPrompt<int>),
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text("Please enter your age."),
                     RetryPrompt = MessageFactory.Text("You are not eligible to vote, Age must be greater than 18 and less than 100."),
                 },
                cancellationToken);
        }

        private async Task<DialogTurnResult> VoterStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["age"] = (int)stepContext.Result;
            return await stepContext.PromptAsync(nameof(TextPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Enter Your Voter Id Number "),
                    RetryPrompt = MessageFactory.Text("The voter id number length must be equals to 10."),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["voterid"] = (string)stepContext.Result;
            return await stepContext.PromptAsync(nameof(ConfirmPrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Do you wanna save")
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var user = await _UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfileClass(), cancellationToken);
                user.Gender = (string)stepContext.Values["gender"];
                user.Name = (string)stepContext.Values["name"];
                user.Age = (int)stepContext.Values["age"];
                user.Voter = (string)stepContext.Values["voterid"];
                String age = Convert.ToString(user.Age);
                //var msg = $"Your Name is : {user.Name}, gender is : {user.Gender}, age is : {user.Age}, voter id number : {user.Voter} ";
                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(AdCard(user.Name, user.Gender, age, user.Voter)), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thanks. Your profile will not be kept."), cancellationToken);
            }
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private Task<bool> AgeValidatorAsync(PromptValidatorContext<int> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(promptContext.Recognized.Succeeded && promptContext.Recognized.Value > 17 && promptContext.Recognized.Value < 100);
        }

        public static Attachment AdCard(String name, String gender, String Age, String voterid)
        {
            AdaptiveCard card = new AdaptiveCard("1.2")
            {
                Body = new List<AdaptiveElement>()
                {
                    new AdaptiveTextBlock
                    {
                        Text="VOTER ID",
                        Size=AdaptiveTextSize.Large,
                        Color=AdaptiveTextColor.Good,
                        HorizontalAlignment=AdaptiveHorizontalAlignment.Center,
                    },
                    new AdaptiveColumnSet
                    {
                        Columns=new List<AdaptiveColumn>()
                        {
                            new AdaptiveColumn
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text="Id :",
                                        Size=AdaptiveTextSize.Large,
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text="Name :",
                                        Size=AdaptiveTextSize.Large,
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text="Age :",
                                        Size=AdaptiveTextSize.Large,
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text="Gender :",
                                        Size=AdaptiveTextSize.Large,
                                    }
                                  
                                }
                            },
                            new AdaptiveColumn
                            {
                                Items=new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text=voterid,
                                        Size=AdaptiveTextSize.Large,
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text=name,
                                        Size=AdaptiveTextSize.Large,
                                    },
                                   new AdaptiveTextBlock
                                    {
                                        Text=Age,
                                        Size=AdaptiveTextSize.Large,
                                    },
                                   new AdaptiveTextBlock
                                    {
                                        Text=gender,
                                        Size=AdaptiveTextSize.Large,
                                    }
                                }
                            },

                        }

                    },


                }
            };

            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

            return attachment;
        }
    }
}

