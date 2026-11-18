namespace Evolve.Civilization
{
    /// <summary>The eight "Thể" classes of the treefolk (given by the project owner). Each has its own house in the city.</summary>
    public enum TreefolkClass { DauThe, LiepThe, NangThe, QuanThe, DuongThe, XaThe, CauThe, DaThe }

    public static class TreefolkClasses
    {
        public static string Name(TreefolkClass c) => c switch
        {
            TreefolkClass.DauThe => "Đấu Thể",
            TreefolkClass.LiepThe => "Liệp Thể",
            TreefolkClass.NangThe => "Năng Thể",
            TreefolkClass.QuanThe => "Quần Thể",
            TreefolkClass.DuongThe => "Dưỡng Thể",
            TreefolkClass.XaThe => "Xạ Thể",
            TreefolkClass.CauThe => "Cấu Thể",
            _ => "Dã Thể"
        };

        public static string Role(TreefolkClass c) => c switch
        {
            TreefolkClass.DauThe => "Cận chiến, phòng thủ",
            TreefolkClass.LiepThe => "Ám sát, săn mồi",
            TreefolkClass.NangThe => "Năng lượng, nguyên tố",
            TreefolkClass.QuanThe => "Triệu hồi, điều khiển sinh vật",
            TreefolkClass.DuongThe => "Hồi phục, bảo hộ",
            TreefolkClass.XaThe => "Tấn công tầm xa",
            TreefolkClass.CauThe => "Nghiên cứu, cải tạo sinh học",
            _ => "Thám hiểm, sinh tồn"
        };
    }
}
