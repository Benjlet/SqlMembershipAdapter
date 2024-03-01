namespace SqlMembershipAdapter.Models
{
    public class FindUsersRequest
    {
        private string _criteria = string.Empty;
        private int pageIndex;
        private int pageSize;

        public string Criteria
        {
            get => _criteria;
            set => _criteria = value?.Trim() ?? string.Empty;
        }

        public int PageIndex
        {
            get => pageIndex;
            set => pageIndex = value < 0 ? 0 : value;
        }

        public int PageSize
        {
            get => pageSize;
            set => pageSize = value < 1 ? 1 : value;
        }
    }
}