using System.Runtime.Serialization;

namespace ZooKeeper.Mgt.Website.Common
{
    [DataContract]
    public class Pager
    {
        [DataMember]
        public int PageIndex { get; set; }

        [DataMember]
        public int PageSize { get; set; }

        public int Skip => (PageIndex - 1) * PageSize;

        [DataMember]
        public int TotalCount { get; set; }

        int _totalPage;

        [DataMember]
        public int TotalPage
        {
            get
            {
                if (PageSize == 0)
                {
                    PageSize = 10;
                }
                var _totalPage = TotalCount / PageSize;
                if (TotalCount % PageSize != 0) _totalPage++;

                return _totalPage;
            }
            set => _totalPage = value;
        }

        public Pager()
        {
            PageIndex = 1;
            PageSize = 10;
        }

        public Pager(int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
        }

        public Pager(int pageIndex, int pageSize, int totalCount)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = totalCount;
        }
    }
}