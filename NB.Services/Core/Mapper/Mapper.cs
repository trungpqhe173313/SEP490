using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NB.Service.Core.Mapper
{
    public class Mapper : IMapper
    {
        public TDestination Map<TSource, TDestination>(TSource source)
        {
            var destination = Activator.CreateInstance<TDestination>();
            MapObject(source, destination);
            return destination;
        }


        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            MapObject(source, destination);
            return destination;
        }


        public IEnumerable<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> sourceList)
        {
            List<TDestination> destinationList = new List<TDestination>();

            foreach (var source in sourceList)
            {
                TDestination destination = Map<TSource, TDestination>(source);
                destinationList.Add(destination);
            }

            return destinationList;
        }

        private void MapObject<TSource, TDestination>(TSource source, TDestination destination)
        {
            var sourceProperties = typeof(TSource).GetProperties();
            var destinationProperties = typeof(TDestination).GetProperties();

            foreach (var sourceProperty in sourceProperties)
            {
                var destinationProperty = destinationProperties.FirstOrDefault(p =>
                    p.Name == sourceProperty.Name && (p.PropertyType == sourceProperty.PropertyType ||
                    Nullable.GetUnderlyingType(p.PropertyType) == sourceProperty.PropertyType));

                if (destinationProperty != null)
                {
                    var value = sourceProperty.GetValue(source);
                    destinationProperty.SetValue(destination, value);
                }
            }
        }
    }
}
