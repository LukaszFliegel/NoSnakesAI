using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable SKEXP0110, SKEXP0001


Kernel kernel = SetupKernel();

// example 1
await SimpleExample(kernel);

// example 2
//await ParrotExample(kernel);

// example 3
//await ParrotAndOldGrannyExample(kernel);

// example 4
//await TicTacToeExample(kernel);

Console.WriteLine("");
Console.ReadKey();



static async Task SimpleExample(Kernel kernel)
{
    var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

    ChatHistory history = [];
    var userPrompt = "Hello, how are you?";
    history.AddUserMessage(userPrompt);

    PrintUserMessage(userPrompt);

    var response = chatCompletionService.GetStreamingChatMessageContentsAsync(
        chatHistory: history,
        kernel: kernel
    );

    await PrintBotStreamedResponse(response);
}


static async Task ParrotExample(Kernel kernel)
{
    ChatCompletionAgent agent =
                new()
                {
                    Name = "Parrot",
                    Instructions = "Repeat the user message in the voice of a pirate and then end with a parrot sound.",
                    Kernel = kernel
                };

    /// Create the chat history to capture the agent interaction.
    ChatHistory chat = [];

    // Respond to user input
    await InvokeAgentAsync("Fortune favors the bold.");
    await InvokeAgentAsync("I came, I saw, I conquered.");
    await InvokeAgentAsync("Practice makes perfect.");

    // Local function to invoke agent and display the conversation messages.
    async Task InvokeAgentAsync(string input)
    {
        ChatMessageContent message = new(AuthorRole.User, input);
        chat.Add(message);
        PrintUserMessage(input);

        await foreach (ChatMessageContent response in agent.InvokeAsync(chat))
        {
            chat.Add(response);

            PrintBotResponse(response.ToString());
        }
    }
}


static async Task ParrotAndOldGrannyExample(Kernel kernel)
{
    ChatCompletionAgent parrotAgent =
                new()
                {
                    Name = "Parrot",
                    Instructions = "Act as a irytating parrot named \"Polly\".\n" +
                        "When your owner - an old granny - will say somethign repeat what she just said.\n" +
                        "Repeat it in the voice of a pirate and then end with a parrot sound.\n" +
                        "Do not respond to any user message, just repeat what granny said and only if she say something.",
                    Kernel = kernel
                };

    ChatCompletionAgent oldGrannyAgent =
                new()
                {
                    Name = "Granny",
                    Instructions = "Act like an old granny and give advice to the user. You own a parrot named \"Polly\".\n" +
                        "Your parrot iritates you when it repeats what you say or when it pretends to be a pirate.\n" +
                        "Each time parrot repeats what you just said or pretends to be a pirate makes you more irytated.\n" +
                        "Try to make the parrot stop repeating you or pretending to be a pirate.\n" +
                        "After 4 or more such messages if parrot will not listen, say \"Leave me alone\" and don't answer anymore." +
                        "Respond very briefly - the less words the better.",
                    Kernel = kernel                    
                };

    // Create a chat for agent interaction.
    AgentGroupChat oldGrannyHouseChat =
        new(oldGrannyAgent, parrotAgent)
        {
            ExecutionSettings =
                new()
                {
                    // Here a TerminationStrategy subclass is used that will terminate when
                    // an assistant message contains the term "approve".
                    TerminationStrategy =
                        new GrannyIrytationTerminationStrategy()
                        {
                            // Only the granny can be irytated
                            Agents = [oldGrannyAgent],
                            // Limit total number of turns
                            MaximumIterations = 24,
                        }
                }
        };

    // Invoke chat and display messages.
    ChatMessageContent input = new(AuthorRole.User, "How are you today granny?");
    oldGrannyHouseChat.AddChatMessage(input);
    PrintUserMessage(input.ToString());

    await foreach (ChatMessageContent response in oldGrannyHouseChat.InvokeAsync())
    {        
        if(response.AuthorName == "Granny")
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"[{response.AuthorName}]: ");
            Console.WriteLine(response.Content);
        }
        else if (response.AuthorName == "Parrot")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[{response.AuthorName}]: ");
            Console.WriteLine(response.Content);
        }

        Console.ResetColor();
    }
}


static async Task TicTacToeExample(Kernel kernel)
{
    string playerInstructions = $$$"""
        You are a professional player in Tic-Tac-Toe.
        {0}
        You will be presented state of a game with Tic-Tac-Toe board, respond with a next move.
        Respond with only board state in form of:
        _ | _ | _
        _ | _ | _
        _ | _ | _
        example:
        o | o | _
        _ | _ | x
        x | _ | _
        where x and o are signs of players and _ is an empty field.
        """;

    string refereeInstructions = $$$"""
        You are a professional referee in Tic-Tac-Toe.
        There are two players your are managing - "circle" and "cross".
        Players will be giving the board state in for of a:
        _ | _ | _
        _ | _ | _
        _ | _ | _
        as an example:
        o | o | _
        _ | _ | x
        x | _ | _
        where x and o are signs of players and _ is an empty field.
        Your goal is to manage the game state, i.e. call what player should make a move, call when game is finish and give name of a winner.
        If anyone will make an illegal move (like putting two signs or changing already set sign) call it immediatelly.
        If one player will win by settings their three signs in a row (horizontally, vertically or diagonally) call it immediatelly.
        Whenever game shall end (because of winner or illegal move) say "[FINISH]" (including square brackets) and after this give explanation why game is finished.
        At the start of conversation pick one player ("circle" or "cross") to make a move.
        Remember, that you are a referee, do not make any changes to the board state. Do not repeat board state, just say who's turn it is or when it's FINISH.
        """;

    string selectorInstructions = $$$"""
        Your job is to determine which participant takes the next turn in a conversation according to the action of the most recent participant.
        State only the name of the participant to take the next turn.

        Choose only from these participants:
        - "circle"
        - "cross"
        - "referee"

        Always follow these steps when selecting the next participant:
        1) After user input, it is "referee" turn.
        2) After "circle" or "cross" replies, it's "referee" turn.
        3) If before "referee", "circle" replied, then it's "cross" turn.
        4) If before "referee", "cross" replied, then it's "circle" turn.
        5) If "referee" says "[FINISH]", the conversation is over.
        6) If before "referee", user replied, then it's either "circle" or "cross" turn, depending on "referee" instructions.
     
        History:
        {{$history}}
        """;    

    ChatCompletionAgent circlePlayer = new()
    {
        Name = "circle",
        Instructions = string.Format(playerInstructions, "You use circles - \"o\"."),
        Kernel = kernel
    };

    ChatCompletionAgent crossPlayer = new()
    {
        Name = "cross",
        Instructions = string.Format(playerInstructions, "You use crosses \"x\"."),
        Kernel = kernel
    };    

    ChatCompletionAgent referee = new()
    {
        Name = "referee",
        Instructions = refereeInstructions,
        Kernel = kernel
    };

    KernelFunction selectionFunction = KernelFunctionFactory.CreateFromPrompt(selectorInstructions);

    // Create a chat for agent interaction.
    AgentGroupChat gameChat = new(referee, crossPlayer, circlePlayer)
    {
        ExecutionSettings = new()
        {
            TerminationStrategy = new TicTacToeTerminationStrategy()
            {
                Agents = [referee],
                MaximumIterations = 24                          
            },
            SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, kernel)
            {
                HistoryVariableName = "history"
            }
        }
    };

    // Invoke chat and display messages.
    ChatMessageContent input = new(AuthorRole.User, "Please start the game.");
    gameChat.AddChatMessage(input);
    PrintUserMessage(input.ToString());

    // print group chat on console
    await foreach (ChatMessageContent response in gameChat.InvokeAsync())
    {
        if (response.AuthorName == "cross")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{response.AuthorName}]: ");
            Console.WriteLine(response.Content);
        }
        else if (response.AuthorName == "circle")
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"[{response.AuthorName}]: ");
            Console.WriteLine(response.Content);
        }
        else if (response.AuthorName == "referee")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{response.AuthorName}]: ");
            Console.WriteLine(response.Content);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"[{response.AuthorName}]: ");
            Console.WriteLine(response.Content);
        }

        Console.ResetColor();
    }
}

static Kernel SetupKernel()
{
    IConfiguration configuration = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();

    IKernelBuilder kernelBuilder = Kernel.CreateBuilder();

    kernelBuilder.AddAzureOpenAIChatCompletion(
                    configuration["AzureOpenAI:DeploymentName"],
                    configuration["AzureOpenAI:Endpoint"],
                    configuration["AzureOpenAI:ApiKey"]);

    Console.WriteLine("Kernel initialized\n");

    return kernelBuilder.Build();
}

static void PrintUserMessage(string userPrompt)
{
    Console.ForegroundColor = ConsoleColor.Blue;
    Console.WriteLine($"[User]: {userPrompt}");
    Console.ResetColor();
}

static async Task PrintBotStreamedResponse(IAsyncEnumerable<StreamingChatMessageContent> response)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write($"[Bot]: ");
    await foreach (var chunk in response)
    {
        Console.Write(chunk);
    }
    Console.Write("\n");
    Console.ResetColor();
}

static void PrintBotResponse(string response)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"[Bot]: {response}");
    Console.ResetColor();
}

class GrannyIrytationTerminationStrategy : TerminationStrategy
{
    // Terminate when the message contains the term "Leave me alone"
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        => Task.FromResult(history[history.Count - 1].Content?.Contains("Leave me alone", StringComparison.OrdinalIgnoreCase) ?? false);
}

class TicTacToeTerminationStrategy : TerminationStrategy
{
    // Terminate when the message contains the term "[FINISH]"
    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
        => Task.FromResult(history[history.Count - 1].Content?.Contains("[FINISH]", StringComparison.OrdinalIgnoreCase) ?? false);
}

#pragma warning restore SKEXP0110, SKEXP0001