using Refit;

namespace MauiInsights.App
{
    public interface IRestClient
    {
        [Get("/todos/{id}")]
        Task<Todo> GetTodo(int id);
    }

    public record Todo(int UserId, int Id, string Title, bool Completed);
}
