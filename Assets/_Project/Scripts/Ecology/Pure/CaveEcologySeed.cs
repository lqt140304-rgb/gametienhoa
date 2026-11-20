using Evolve.Creatures;

namespace Evolve.Ecology
{
    /// <summary>
    /// The ecosystem of the 11 caves with creatures (spec 5.2 food chains, 5.4 numbers), as data: pools of life, 2 prey, 1 hunter and 1 cave lord per cave,
    /// who eats what, and the tunnels between caves (placeholder until the maze's real graph is available). All numbers are provisional.
    /// Prey escape chances come from PreyDefenseSeed. Build() makes a fresh world each time.
    /// </summary>
    public static class CaveEcologySeed
    {
        // role defaults (tuned by the 2-hour simulation in the tests)
        const float PreyMeals = 0.12f, HunterMeals = 0.30f, LordMeals = 0.15f;

        static DietEntry Pool(string id, float w = 1f) => new DietEntry { Id = id, Weight = w, IsPool = true };
        static DietEntry Eat(string id, float w = 1f) => new DietEntry { Id = id, Weight = w };
        static DietEntry Visit(string region, string id, float eff, float w = 1f) => new DietEntry { Id = id, Weight = w, RegionId = region, Efficiency = eff };

        static SpeciesPopulation Prey(string id, params DietEntry[] diet)
        {
            var s = new SpeciesPopulation
            {
                Id = id, Count = 8, Capacity = 10, Dens = 2, BirthsPerMinute = 0.35f, MealsPerHeadPerMinute = diet.Length == 0 ? 0f : PreyMeals, RecoverMinutes = 6f, BodyFood = 0.5f,   // Trứng Lạ is an egg: no food
                Migrates = true, EscapeChance = PreyDefenseSeed.Get(id)?.EscapeChance ?? 0f,
            };
            s.Diet.AddRange(diet);
            return s;
        }

        static SpeciesPopulation Hunter(string id, params DietEntry[] diet)
        {
            var s = new SpeciesPopulation { Id = id, Count = 2, Capacity = 3, Dens = 1, BirthsPerMinute = 0.15f, MealsPerHeadPerMinute = HunterMeals, Floor = 1, RecoverMinutes = 6f, BodyFood = 1f };
            s.Diet.AddRange(diet);
            return s;
        }

        /// <summary>One cave lord: never starves to death, comes back from the nest 9 minutes after it is killed.</summary>
        static SpeciesPopulation Lord(string id, params DietEntry[] diet)
        {
            var s = new SpeciesPopulation { Id = id, Count = 1, Capacity = 1, Dens = 1, MealsPerHeadPerMinute = diet.Length == 0 ? 0f : LordMeals, Floor = 1, RecoverMinutes = 9f, BodyFood = 3f };
            s.Diet.AddRange(diet);
            return s;
        }

        static RegionPopulation Cave(EcologyWorld w, string id, string mainPool, params SpeciesPopulation[] species)
        {
            var r = w.Add(id, new RegionPopulation { ReturnPool = mainPool });
            r.AddPool(mainPool, 40f, 3f, 60f);
            foreach (var s in species) r.Add(s);
            return r;
        }

        public static readonly string[] CaveIds =
        {
            "khoi_sinh", "vuon_quang", "reu_tho", "to_bo_hoang", "vuc_nut_tim", "nam_huynh_quang", "suoi_nuoc_nong", "khe_gio_hu", "nghia_dia", "hoa_thach", "tieng_tho",
        };

        public static EcologyWorld Build()
        {
            var w = new EcologyWorld();

            Cave(w, "khoi_sinh", "reu_ven_ho",
                Prey("bo_canh_cung", Pool("reu_ven_ho")), Prey("oc_ho", Pool("reu_ven_ho")),
                Hunter("tham_lan_bo_ho", Eat("bo_canh_cung"), Eat("oc_ho", 0.3f)),
                Lord("coc_ho_gia", Eat("tham_lan_bo_ho"), Eat("bo_canh_cung")));

            var vuon = Cave(w, "vuon_quang", "la_sang",
                Prey("tho_la", Pool("la_sang")), Prey("dom_dom", Pool("mat_hoa")),
                Hunter("bo_giap_quang", Eat("dom_dom")),
                Lord("rua_pha_le", Pool("quang"), Eat("bo_giap_quang", 0.5f)));
            vuon.AddPool("mat_hoa", 40f, 3f, 60f); vuon.AddPool("quang", 40f, 3f, 60f);

            Cave(w, "reu_tho", "reu",
                Prey("sen_reu", Pool("reu")), Prey("muoi_reu", Pool("reu")),
                Hunter("ret_reu", Eat("sen_reu"), Eat("muoi_reu", 0.3f)),
                Lord("reu_me", Pool("reu")));

            Cave(w, "to_bo_hoang", "hat_to",
                Prey("chim_non", Pool("hat_to")), Prey("trung_la"),
                Hunter("ran_trom_trung", Eat("trung_la"), Eat("chim_non", 0.5f)),
                Lord("chim_hang_me", Visit("khe_gio_hu", "de_gio", 0.6f), Visit("khe_gio_hu", "chuot_gio", 0.6f)));

            Cave(w, "vuc_nut_tim", "tinh_the_vun",
                Prey("bo_gai_tim", Pool("tinh_the_vun")),
                Prey("doi_am_thoa", Visit("nam_huynh_quang", "buom_bao_tu", 0.5f), Visit("tieng_tho", "buom_dem", 0.5f)),
                Hunter("nhen_tinh_the", Eat("doi_am_thoa")),
                Lord("bo_cap_thach_anh", Eat("nhen_tinh_the"), Eat("bo_gai_tim")));

            Cave(w, "nam_huynh_quang", "bao_tu",
                Prey("nam_di_bo", Pool("bao_tu")), Prey("buom_bao_tu", Pool("bao_tu")),
                Hunter("ech_doc_dom", Eat("buom_bao_tu"), Eat("nam_di_bo", 0.5f)),
                Lord("nam_khong_lo", Pool("bao_tu")));

            Cave(w, "suoi_nuoc_nong", "tao_nong",
                Prey("oc_luu_huynh", Pool("tao_nong")), Prey("cua_da_nong", Eat("oc_luu_huynh"), Pool("tao_nong", 0.5f)),
                Hunter("ky_nhong_lua", Eat("cua_da_nong")),
                Lord("ran_dung_nham", Eat("ky_nhong_lua"), Eat("cua_da_nong", 0.5f)));

            Cave(w, "khe_gio_hu", "hat_gio",
                Prey("de_gio", Pool("hat_gio")), Prey("chuot_gio", Pool("hat_gio")),
                Hunter("chon_khe", Eat("chuot_gio"), Eat("de_gio", 0.5f)),
                Lord("cao_gio", Eat("chon_khe", 0.5f), Eat("chuot_gio"), Visit("to_bo_hoang", "chim_non", 0.4f, 0.5f)));

            Cave(w, "nghia_dia", "xac",
                Prey("gioi_trang", Pool("xac")), Prey("ken_ken_hang", Pool("xac")),
                Hunter("bo_an_xac", Eat("gioi_trang"), Pool("xac", 0.5f)),
                Lord("cho_xuong", Eat("bo_an_xac", 0.5f), Eat("gioi_trang", 0.5f), Pool("xac")));

            Cave(w, "hoa_thach", "dia_y",
                Prey("bo_da", Pool("dia_y")), Prey("tac_ke_da", Eat("bo_da")),
                Hunter("slime_da", Eat("tac_ke_da")),
                Lord("golem_thach_anh"));

            Cave(w, "tieng_tho", "reu_toi",
                Prey("buom_dem", Pool("reu_toi")), Prey("ret_mu", Eat("buom_dem")),
                Hunter("ke_rinh_bong_toi", Eat("ret_mu")),
                Lord("bong_hut", Eat("ke_rinh_bong_toi", 0.5f), Eat("ret_mu", 0.5f)));

            // tunnels between caves: a placeholder chain until the maze graph exists
            foreach (var t in new[]
            {
                ("khoi_sinh", "vuon_quang"), ("khoi_sinh", "reu_tho"), ("khoi_sinh", "to_bo_hoang"), ("vuon_quang", "vuc_nut_tim"),
                ("reu_tho", "nam_huynh_quang"), ("to_bo_hoang", "khe_gio_hu"), ("vuc_nut_tim", "nam_huynh_quang"), ("vuc_nut_tim", "tieng_tho"),
                ("nam_huynh_quang", "suoi_nuoc_nong"), ("suoi_nuoc_nong", "khe_gio_hu"), ("khe_gio_hu", "nghia_dia"), ("nghia_dia", "hoa_thach"), ("hoa_thach", "tieng_tho"),
            }) w.Link(t.Item1, t.Item2);
            return w;
        }
    }
}
