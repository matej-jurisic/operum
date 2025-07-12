using Operum.Service.Mappings.Profiles;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Operum.Service.Mappings.Mapper
{
    public class Mapper : IMapper
    {
        private static readonly ConcurrentDictionary<(Type Source, Type Dest), Delegate> _mappings = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

        public Mapper()
        {
        }

        public Mapper(IEnumerable<IMappingProfile> profiles)
        {
            foreach (var profile in profiles)
            {
                profile.RegisterMappings(this);
            }
        }

        public void Register<TSource, TDestination>(Action<TSource, TDestination>? manualMap = null)
            where TDestination : new()
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            _mappings[(sourceType, destinationType)] = CreateMappingFunction<TSource, TDestination>(manualMap);
        }

        public TDestination Map<TSource, TDestination>(TSource source)
        {
            if (source == null)
                return default!;

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            // Check for direct mapping
            if (_mappings.TryGetValue((sourceType, destinationType), out var del) &&
                del is Func<TSource, TDestination> mapFunc)
            {
                return mapFunc(source);
            }

            // Handle enumerable types
            if (IsEnumerableType(sourceType, out var sourceElementType) &&
                IsEnumerableType(destinationType, out var destinationElementType))
            {
                return MapEnumerable<TSource, TDestination>(source, sourceElementType!, destinationElementType!);
            }

            throw new InvalidOperationException($"No mapping registered for {sourceType.Name} → {destinationType.Name}");
        }

        /// <summary>
        /// Maps source object to an existing destination object
        /// </summary>
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (source == null)
                return destination;

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            // Check if we have a registered mapping and try to extract the manual mapping action
            if (_mappings.TryGetValue((sourceType, destinationType), out var del) &&
                del is Func<TSource, TDestination>)
            {
                // For registered mappings, we need to perform the property mapping manually
                // since the registered function creates a new instance
                MapPropertiesToExisting(source, destination);

                // If there was a manual mapping action in the registration, we can't easily extract it
                // This is a limitation of the current design - consider refactoring if needed
                return destination;
            }

            // Fallback to direct property mapping
            MapPropertiesToExisting(source, destination);
            return destination;
        }

        /// <summary>
        /// Maps source object to an existing destination object with custom mapping action
        /// </summary>
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination, Action<TSource, TDestination> customMapping)
        {
            if (source == null)
                return destination;

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            // First do the standard property mapping
            MapPropertiesToExisting(source, destination);

            // Then apply custom mapping
            customMapping?.Invoke(source, destination);

            return destination;
        }

        private void MapPropertiesToExisting<TSource, TDestination>(TSource source, TDestination destination)
        {
            var sourceProps = GetCachedProperties(typeof(TSource));
            var destProps = GetCachedProperties(typeof(TDestination));

            // Create a dictionary for faster property lookup
            var destPropDict = destProps.Where(p => p.CanWrite)
                                      .ToDictionary(p => p.Name, p => p);

            foreach (var sourceProp in sourceProps)
            {
                if (destPropDict.TryGetValue(sourceProp.Name, out var destProp) &&
                    IsCompatibleType(sourceProp.PropertyType, destProp.PropertyType))
                {
                    try
                    {
                        var value = sourceProp.GetValue(source);
                        destProp.SetValue(destination, value);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to map property '{sourceProp.Name}' from {typeof(TSource).Name} to {typeof(TDestination).Name}", ex);
                    }
                }
            }
        }

        private Func<TSource, TDestination> CreateMappingFunction<TSource, TDestination>(Action<TSource, TDestination>? manualMap)
            where TDestination : new()
        {
            var sourceProps = GetCachedProperties(typeof(TSource));
            var destProps = GetCachedProperties(typeof(TDestination));

            // Create a dictionary for faster property lookup
            var destPropDict = destProps.Where(p => p.CanWrite)
                                      .ToDictionary(p => p.Name, p => p);

            return source =>
            {
                var dest = new TDestination();

                foreach (var sourceProp in sourceProps)
                {
                    if (destPropDict.TryGetValue(sourceProp.Name, out var destProp) &&
                        IsCompatibleType(sourceProp.PropertyType, destProp.PropertyType))
                    {
                        try
                        {
                            var value = sourceProp.GetValue(source);
                            destProp.SetValue(dest, value);
                        }
                        catch (Exception ex)
                        {
                            // Log or handle mapping errors as needed
                            throw new InvalidOperationException(
                                $"Failed to map property '{sourceProp.Name}' from {typeof(TSource).Name} to {typeof(TDestination).Name}", ex);
                        }
                    }
                }

                manualMap?.Invoke(source, dest);
                return dest;
            };
        }

        private TDestination MapEnumerable<TSource, TDestination>(TSource source, Type sourceElementType, Type destinationElementType)
        {
            if (source is not IEnumerable sourceEnumerable)
                throw new InvalidOperationException("Source is not enumerable");

            var mapMethod = typeof(Mapper).GetMethod(nameof(MapEnumerableInternal), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(sourceElementType, destinationElementType);

            var result = mapMethod.Invoke(this, [sourceEnumerable]);

            // Handle different collection types
            var destinationType = typeof(TDestination);

            if (destinationType.IsArray)
            {
                var toArrayMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray))!
                    .MakeGenericMethod(destinationElementType);
                return (TDestination)toArrayMethod.Invoke(null, [result])!;
            }

            if (destinationType.IsGenericType)
            {
                var genericTypeDef = destinationType.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(List<>))
                {
                    var toListMethod = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList))!
                        .MakeGenericMethod(destinationElementType);
                    return (TDestination)toListMethod.Invoke(null, [result])!;
                }
            }

            return (TDestination)result!;
        }

        private IEnumerable<TDestinationElement> MapEnumerableInternal<TSourceElement, TDestinationElement>(IEnumerable source)
        {
            foreach (var item in source)
            {
                if (item is TSourceElement sourceItem)
                {
                    yield return Map<TSourceElement, TDestinationElement>(sourceItem);
                }
            }
        }

        private static PropertyInfo[] GetCachedProperties(Type type)
        {
            return _propertyCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        }

        private static bool IsEnumerableType(Type type, out Type? elementType)
        {
            elementType = null;

            // Don't treat string as enumerable
            if (type == typeof(string))
                return false;

            // Check if it's an array
            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }

            // Check if it implements IEnumerable<T>
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(IEnumerable<>) ||
                    genericTypeDef == typeof(IList<>) ||
                    genericTypeDef == typeof(ICollection<>) ||
                    genericTypeDef == typeof(List<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            // Check interfaces
            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface != null)
            {
                elementType = enumerableInterface.GetGenericArguments()[0];
                return true;
            }

            return false;
        }

        private static bool IsCompatibleType(Type sourceType, Type destinationType)
        {
            if (sourceType == destinationType)
                return true;

            if (destinationType.IsAssignableFrom(sourceType))
                return true;

            // Handle nullable types
            var sourceNullable = Nullable.GetUnderlyingType(sourceType);
            var destNullable = Nullable.GetUnderlyingType(destinationType);

            if (sourceNullable != null && destNullable != null)
                return sourceNullable == destNullable;

            if (sourceNullable != null)
                return sourceNullable == destinationType;

            if (destNullable != null)
                return sourceType == destNullable;

            return false;
        }

        /// <summary>
        /// Creates a mapping registration dynamically at runtime for types that support parameterless construction
        /// </summary>
        public void RegisterDynamic(Type sourceType, Type destinationType, Delegate? customMapping = null)
        {
            if (destinationType.IsAbstract || destinationType.GetConstructor(Type.EmptyTypes) == null)
                throw new ArgumentException($"Destination type {destinationType.Name} must have a public parameterless constructor");

            var createMappingMethod = typeof(Mapper).GetMethod(nameof(CreateDynamicMapping), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(sourceType, destinationType);

            var mappingFunc = createMappingMethod.Invoke(this, [customMapping]);
            _mappings[(sourceType, destinationType)] = (Delegate)mappingFunc!;
        }

        private Func<TSource, TDestination> CreateDynamicMapping<TSource, TDestination>(Delegate? customMapping)
        {
            var sourceProps = GetCachedProperties(typeof(TSource));
            var destProps = GetCachedProperties(typeof(TDestination));

            var destPropDict = destProps.Where(p => p.CanWrite)
                                      .ToDictionary(p => p.Name, p => p);

            return source =>
            {
                var dest = Activator.CreateInstance<TDestination>();

                foreach (var sourceProp in sourceProps)
                {
                    if (destPropDict.TryGetValue(sourceProp.Name, out var destProp) &&
                        IsCompatibleType(sourceProp.PropertyType, destProp.PropertyType))
                    {
                        try
                        {
                            var value = sourceProp.GetValue(source);
                            destProp.SetValue(dest, value);
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                $"Failed to map property '{sourceProp.Name}' from {typeof(TSource).Name} to {typeof(TDestination).Name}", ex);
                        }
                    }
                }

                if (customMapping is Action<TSource, TDestination> action)
                {
                    action.Invoke(source, dest);
                }

                return dest;
            };
        }
    }
}