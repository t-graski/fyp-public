namespace backend.services.interfaces;

public interface IBootstrapService
{
    Task Boostrap();

    Task Propagate();
}