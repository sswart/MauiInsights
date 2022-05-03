using Microsoft.Extensions.Logging;

namespace MauiInsights.App;

public partial class MainPage : ContentPage
{
	int count = 0;
    private readonly IRestClient _restClient;
    private readonly ILogger<MainPage> _logger;

    public MainPage(IRestClient restClient, ILogger<MainPage> logger)
	{
		InitializeComponent();
        _restClient = restClient;
        _logger = logger;
    }

	private void OnCounterClicked(object sender, EventArgs e)
	{
		if (count > 5)
        {
			throw new ApplicationException("Count is too high!");
        }

		count++;
		CounterLabel.Text = $"Current count: {count}";

		SemanticScreenReader.Announce(CounterLabel.Text);
		
	}

    private async void Button_Clicked(object sender, EventArgs e)
    {
		var result = await _restClient.GetTodo(1);
		_logger.LogInformation("Got todo {todo}", result);
    }
}

