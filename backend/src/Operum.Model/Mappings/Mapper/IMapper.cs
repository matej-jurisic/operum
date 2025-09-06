namespace Operum.Service.Mappings.Mapper
{
    public interface IMapper
    {

        void Register<TSource, TDestination>(Action<TSource, TDestination>? manualMap = null)
            where TDestination : new();
        void RegisterDynamic(Type sourceType, Type destinationType, Delegate? customMapping = null);
        void RegisterCrud<TModel, TDto, TCreateDto, TUpdateDto>()
            where TModel : new()
            where TDto : new();

        TDestination Map<TSource, TDestination>(TSource source);
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination, Action<TSource, TDestination> customMapping);
    }
}