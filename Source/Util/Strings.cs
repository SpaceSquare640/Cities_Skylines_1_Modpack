using System.Collections.Generic;

namespace AutomatedPublicTransportPlanning.Util
{
    /// <summary>
    /// Every user-facing string in the mod, in each language it has been translated
    /// into. English is the source language; the rest are additions to it.
    ///
    /// Adding a language is one method plus one line in BuildTables. Correcting a
    /// translation is one line in one method — the tables are grouped by language
    /// rather than by key so that a contributor who speaks one of them can find their
    /// block, change it, and touch nothing else.
    ///
    /// TRANSLATION STATUS: English is authoritative. The other ten were not written by
    /// native speakers and are offered as a starting point; corrections are welcome and
    /// wanted. See the project README for how to send one.
    ///
    /// The mod's NAME is deliberately absent. It is how the mod is found on the
    /// workshop and how players refer to it to each other, so it stays in English in
    /// every language, the way software product names usually do.
    /// </summary>
    public static class Strings
    {
        /// <summary>
        /// Language tags, in the order a picker should offer them. The stored setting
        /// and the dropdown both read this, so they cannot drift apart.
        /// </summary>
        internal static readonly string[] LanguageOrder =
        {
            "en", "zh_TW", "zh_CN", "ja", "ko", "de", "fr", "es", "pt-BR", "ru", "pl"
        };

        /// <summary>
        /// What each language calls itself, in the same order.
        ///
        /// These are NOT translated and must never be. Someone looking for their own
        /// language needs to find it written the way they write it — rendering the list
        /// in whatever language the panel happens to be in right now would hide the very
        /// entry they are hunting for.
        /// </summary>
        internal static readonly string[] LanguageNames =
        {
            "English", "繁體中文", "简体中文", "日本語", "한국어",
            "Deutsch", "Français", "Español", "Português", "Русский", "Polski"
        };

        public const string LabelLanguage = "label.language";
        public const string ModDescription = "mod.description";
        public const string GroupDiagnostics = "group.diagnostics";
        public const string ButtonTestLog = "btn.testLog";
        public const string ButtonBusPrefabs = "btn.busPrefabs";
        public const string ButtonMeasure = "btn.measure";
        public const string ButtonSnapshot = "btn.snapshot";
        public const string ButtonGraph = "btn.graph";
#if DEVTOOLS
        public const string ButtonPlanningPreview = "btn.planningPreview";
        public const string LabelPlanningStatus = "label.planningStatus";
        public const string SnapshotStaleNote = "note.snapshotStale";
#endif
#if DEVTOOLS
        public const string GroupSpike = "group.spike";
        public const string CheckSpikeArm = "check.spikeArm";
        public const string ButtonSpikeCreate = "btn.spikeCreate";
        public const string ButtonSpikeReport = "btn.spikeReport";
        public const string ButtonSpikeRemove = "btn.spikeRemove";
#endif

        internal static Dictionary<string, Dictionary<string, string>> BuildTables()
        {
            Dictionary<string, Dictionary<string, string>> tables =
                new Dictionary<string, Dictionary<string, string>>();

            tables["en"] = English();
            tables["zh_TW"] = TraditionalChinese();
            tables["zh_CN"] = SimplifiedChinese();
            tables["ja"] = Japanese();
            tables["ko"] = Korean();
            tables["de"] = German();
            tables["fr"] = French();
            tables["es"] = Spanish();
            tables["pt-BR"] = Portuguese();
            tables["ru"] = Russian();
            tables["pl"] = Polish();

            return tables;
        }

        // ------------------------------------------------------------- English (source)

        private static Dictionary<string, string> English()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Language";
            d[ModDescription] = "Early preview. Surveys your city's road and transit network to prepare automatic bus-line planning. Read-only: it does not change your city.";
            d[GroupDiagnostics] = "Diagnostics";
            d[ButtonTestLog] = "Write a test line to the log";
            d[ButtonBusPrefabs] = "List bus prefabs in detail";
            d[ButtonMeasure] = "Measure planning costs (brief pause)";
            d[ButtonSnapshot] = "Capture a world snapshot";
            d[ButtonGraph] = "Build the route graph";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "Generate a read-only planning preview";
            d[LabelPlanningStatus] = "Planning preview status";
            d[SnapshotStaleNote] = "Snapshot may become stale after city changes.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - save round trip (writes to your city)";
            d[CheckSpikeArm] = "Enable these buttons - they change the city you have loaded";
            d[ButtonSpikeCreate] = "1. Create a test bus line";
            d[ButtonSpikeReport] = "2. Report test lines";
            d[ButtonSpikeRemove] = "3. Remove test lines";
#endif
            return d;
        }

        // ------------------------------------------------------------- zh_TW 繁體中文

        private static Dictionary<string, string> TraditionalChinese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "語言";
            d[ModDescription] = "早期預覽版。調查城市的道路與大眾運輸網絡，為自動公車路線規劃作準備。唯讀：不會更動你的城市。";
            d[GroupDiagnostics] = "診斷";
            d[ButtonTestLog] = "將測試路線寫入記錄檔";
            d[ButtonBusPrefabs] = "列出公車 prefab 詳細資料";
            d[ButtonMeasure] = "量測規劃成本（會短暫停頓）";
            d[ButtonSnapshot] = "擷取世界快照";
            d[ButtonGraph] = "建立路線圖";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "產生唯讀規劃預覽";
            d[LabelPlanningStatus] = "規劃預覽狀態";
            d[SnapshotStaleNote] = "城市變動後，快照可能已過期。";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - 存檔往返測試（會寫入你的城市）";
            d[CheckSpikeArm] = "啟用這些按鈕 - 它們會變更你已載入的城市";
            d[ButtonSpikeCreate] = "1. 建立測試公車路線";
            d[ButtonSpikeReport] = "2. 回報測試路線";
            d[ButtonSpikeRemove] = "3. 移除測試路線";
#endif
            return d;
        }

        // ------------------------------------------------------------- zh_CN 简体中文

        private static Dictionary<string, string> SimplifiedChinese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "语言";
            d[ModDescription] = "早期预览版。调查城市的道路与公共交通网络，为自动公交线路规划做准备。只读：不会更改你的城市。";
            d[GroupDiagnostics] = "诊断";
            d[ButtonTestLog] = "将测试线路写入日志";
            d[ButtonBusPrefabs] = "列出公交 prefab 详细信息";
            d[ButtonMeasure] = "测量规划开销（会短暂停顿）";
            d[ButtonSnapshot] = "抓取世界快照";
            d[ButtonGraph] = "构建线路图";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "生成只读规划预览";
            d[LabelPlanningStatus] = "规划预览状态";
            d[SnapshotStaleNote] = "城市变化后，快照可能已过期。";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - 存档往返测试（会写入你的城市）";
            d[CheckSpikeArm] = "启用这些按钮 - 它们会更改你已载入的城市";
            d[ButtonSpikeCreate] = "1. 创建测试公交线路";
            d[ButtonSpikeReport] = "2. 报告测试线路";
            d[ButtonSpikeRemove] = "3. 移除测试线路";
#endif
            return d;
        }

        // ------------------------------------------------------------- 日本語

        private static Dictionary<string, string> Japanese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "言語";
            d[ModDescription] = "早期プレビュー版です。自動バス路線計画の準備として、都市の道路網と公共交通網を調査します。読み取り専用で、都市に変更を加えません。";
            d[GroupDiagnostics] = "診断";
            d[ButtonTestLog] = "テスト路線をログに書き出す";
            d[ButtonBusPrefabs] = "バスのプレハブを詳細表示";
            d[ButtonMeasure] = "計画処理のコストを計測（一時的に停止します）";
            d[ButtonSnapshot] = "ワールドスナップショットを取得";
            d[ButtonGraph] = "路線グラフを構築";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "読み取り専用の計画プレビューを生成";
            d[LabelPlanningStatus] = "計画プレビューの状態";
            d[SnapshotStaleNote] = "都市の変更後、スナップショットが古くなっている場合があります。";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - セーブ往復テスト（都市に書き込みます）";
            d[CheckSpikeArm] = "これらのボタンを有効にする - 読み込み中の都市が変更されます";
            d[ButtonSpikeCreate] = "1. テストバス路線を作成";
            d[ButtonSpikeReport] = "2. テスト路線を報告";
            d[ButtonSpikeRemove] = "3. テスト路線を削除";
#endif
            return d;
        }

        // ------------------------------------------------------------- 한국어

        private static Dictionary<string, string> Korean()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "언어";
            d[ModDescription] = "초기 미리보기 버전입니다. 자동 버스 노선 계획을 준비하기 위해 도시의 도로망과 대중교통망을 조사합니다. 읽기 전용이며 도시를 변경하지 않습니다.";
            d[GroupDiagnostics] = "진단";
            d[ButtonTestLog] = "테스트 노선을 로그에 기록";
            d[ButtonBusPrefabs] = "버스 프리팹 상세 목록";
            d[ButtonMeasure] = "계획 비용 측정 (잠시 멈춤)";
            d[ButtonSnapshot] = "월드 스냅샷 캡처";
            d[ButtonGraph] = "노선 그래프 구축";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "읽기 전용 계획 미리보기 생성";
            d[LabelPlanningStatus] = "계획 미리보기 상태";
            d[SnapshotStaleNote] = "도시가 변경되면 스냅샷이 오래된 정보가 될 수 있습니다.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - 저장 왕복 테스트 (도시에 기록됩니다)";
            d[CheckSpikeArm] = "이 버튼들을 활성화 - 불러온 도시가 변경됩니다";
            d[ButtonSpikeCreate] = "1. 테스트 버스 노선 생성";
            d[ButtonSpikeReport] = "2. 테스트 노선 보고";
            d[ButtonSpikeRemove] = "3. 테스트 노선 제거";
#endif
            return d;
        }

        // ------------------------------------------------------------- Deutsch

        private static Dictionary<string, string> German()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Sprache";
            d[ModDescription] = "Frühe Vorschau. Untersucht das Straßen- und Nahverkehrsnetz Ihrer Stadt, um die automatische Buslinienplanung vorzubereiten. Schreibgeschützt: Ihre Stadt wird nicht verändert.";
            d[GroupDiagnostics] = "Diagnose";
            d[ButtonTestLog] = "Testlinie ins Protokoll schreiben";
            d[ButtonBusPrefabs] = "Bus-Prefabs im Detail auflisten";
            d[ButtonMeasure] = "Planungskosten messen (kurze Pause)";
            d[ButtonSnapshot] = "Netzwerk-Momentaufnahme erstellen";
            d[ButtonGraph] = "Routengraph aufbauen";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "Schreibgeschützte Planungsvorschau erstellen";
            d[LabelPlanningStatus] = "Status der Planungsvorschau";
            d[SnapshotStaleNote] = "Nach Änderungen an der Stadt kann der Schnappschuss veraltet sein.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - Speicher-Rundlauf (schreibt in Ihre Stadt)";
            d[CheckSpikeArm] = "Diese Schaltflächen aktivieren - sie verändern die geladene Stadt";
            d[ButtonSpikeCreate] = "1. Testbuslinie erstellen";
            d[ButtonSpikeReport] = "2. Testlinien melden";
            d[ButtonSpikeRemove] = "3. Testlinien entfernen";
#endif
            return d;
        }

        // ------------------------------------------------------------- Français

        private static Dictionary<string, string> French()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Langue";
            d[ModDescription] = "Aperçu préliminaire. Analyse le réseau routier et de transport en commun de votre ville pour préparer la planification automatique des lignes de bus. Lecture seule : votre ville n'est pas modifiée.";
            d[GroupDiagnostics] = "Diagnostic";
            d[ButtonTestLog] = "Écrire une ligne de test dans le journal";
            d[ButtonBusPrefabs] = "Lister les prefabs de bus en détail";
            d[ButtonMeasure] = "Mesurer les coûts de planification (courte pause)";
            d[ButtonSnapshot] = "Capturer un instantané du réseau";
            d[ButtonGraph] = "Construire le graphe des itinéraires";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "Générer un aperçu de planification en lecture seule";
            d[LabelPlanningStatus] = "État de l'aperçu de planification";
            d[SnapshotStaleNote] = "L'instantané peut être obsolète après des changements dans la ville.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - aller-retour de sauvegarde (écrit dans votre ville)";
            d[CheckSpikeArm] = "Activer ces boutons - ils modifient la ville chargée";
            d[ButtonSpikeCreate] = "1. Créer une ligne de bus de test";
            d[ButtonSpikeReport] = "2. Signaler les lignes de test";
            d[ButtonSpikeRemove] = "3. Supprimer les lignes de test";
#endif
            return d;
        }

        // ------------------------------------------------------------- Español

        private static Dictionary<string, string> Spanish()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Idioma";
            d[ModDescription] = "Versión preliminar. Examina la red vial y de transporte público de tu ciudad para preparar la planificación automática de líneas de autobús. Solo lectura: no modifica tu ciudad.";
            d[GroupDiagnostics] = "Diagnóstico";
            d[ButtonTestLog] = "Escribir una línea de prueba en el registro";
            d[ButtonBusPrefabs] = "Listar los prefabs de autobús en detalle";
            d[ButtonMeasure] = "Medir los costes de planificación (breve pausa)";
            d[ButtonSnapshot] = "Capturar una instantánea de la red";
            d[ButtonGraph] = "Construir el grafo de rutas";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "Generar una vista previa de planificación de solo lectura";
            d[LabelPlanningStatus] = "Estado de la vista previa de planificación";
            d[SnapshotStaleNote] = "La instantánea puede quedar obsoleta después de cambios en la ciudad.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - ida y vuelta de guardado (escribe en tu ciudad)";
            d[CheckSpikeArm] = "Activar estos botones - modifican la ciudad que tienes cargada";
            d[ButtonSpikeCreate] = "1. Crear una línea de autobús de prueba";
            d[ButtonSpikeReport] = "2. Informar de las líneas de prueba";
            d[ButtonSpikeRemove] = "3. Eliminar las líneas de prueba";
#endif
            return d;
        }

        // ------------------------------------------------------------- Português

        private static Dictionary<string, string> Portuguese()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Idioma";
            d[ModDescription] = "Prévia inicial. Analisa a rede viária e de transporte público da sua cidade para preparar o planejamento automático de linhas de ônibus. Somente leitura: não altera sua cidade.";
            d[GroupDiagnostics] = "Diagnóstico";
            d[ButtonTestLog] = "Escrever uma linha de teste no log";
            d[ButtonBusPrefabs] = "Listar os prefabs de ônibus em detalhe";
            d[ButtonMeasure] = "Medir os custos de planejamento (breve pausa)";
            d[ButtonSnapshot] = "Capturar um instantâneo da rede";
            d[ButtonGraph] = "Construir o grafo de rotas";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "Gerar uma prévia de planejamento somente leitura";
            d[LabelPlanningStatus] = "Estado da prévia de planejamento";
            d[SnapshotStaleNote] = "O instantâneo pode ficar desatualizado após mudanças na cidade.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - ida e volta de salvamento (escreve na sua cidade)";
            d[CheckSpikeArm] = "Ativar estes botões - eles alteram a cidade carregada";
            d[ButtonSpikeCreate] = "1. Criar uma linha de ônibus de teste";
            d[ButtonSpikeReport] = "2. Relatar as linhas de teste";
            d[ButtonSpikeRemove] = "3. Remover as linhas de teste";
#endif
            return d;
        }

        // ------------------------------------------------------------- Русский

        private static Dictionary<string, string> Russian()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Язык";
            d[ModDescription] = "Ранняя предварительная версия. Изучает дорожную сеть и общественный транспорт города, чтобы подготовить автоматическое планирование автобусных маршрутов. Только чтение: город не изменяется.";
            d[GroupDiagnostics] = "Диагностика";
            d[ButtonTestLog] = "Записать тестовый маршрут в журнал";
            d[ButtonBusPrefabs] = "Подробный список префабов автобусов";
            d[ButtonMeasure] = "Измерить затраты на планирование (короткая пауза)";
            d[ButtonSnapshot] = "Сделать снимок сети";
            d[ButtonGraph] = "Построить граф маршрутов";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "Создать предварительный план только для чтения";
            d[LabelPlanningStatus] = "Состояние предварительного плана";
            d[SnapshotStaleNote] = "После изменений города снимок может устареть.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - цикл сохранения и загрузки (записывает в ваш город)";
            d[CheckSpikeArm] = "Включить эти кнопки - они изменяют загруженный город";
            d[ButtonSpikeCreate] = "1. Создать тестовый автобусный маршрут";
            d[ButtonSpikeReport] = "2. Отчёт по тестовым маршрутам";
            d[ButtonSpikeRemove] = "3. Удалить тестовые маршруты";
#endif
            return d;
        }

        // ------------------------------------------------------------- Polski

        private static Dictionary<string, string> Polish()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d[LabelLanguage] = "Język";
            d[ModDescription] = "Wczesna wersja podglądowa. Analizuje sieć dróg i transportu publicznego w mieście, aby przygotować automatyczne planowanie linii autobusowych. Tylko do odczytu: nie zmienia miasta.";
            d[GroupDiagnostics] = "Diagnostyka";
            d[ButtonTestLog] = "Zapisz linię testową do dziennika";
            d[ButtonBusPrefabs] = "Wypisz szczegóły prefabrykatów autobusów";
            d[ButtonMeasure] = "Zmierz koszty planowania (krótka pauza)";
            d[ButtonSnapshot] = "Wykonaj migawkę sieci";
            d[ButtonGraph] = "Zbuduj graf tras";
#if DEVTOOLS
            d[ButtonPlanningPreview] = "Utwórz podgląd planowania tylko do odczytu";
            d[LabelPlanningStatus] = "Stan podglądu planowania";
            d[SnapshotStaleNote] = "Po zmianach w mieście migawka może być nieaktualna.";
#endif
#if DEVTOOLS
            d[GroupSpike] = "Spike 1 - test zapisu i wczytania (zapisuje w Twoim mieście)";
            d[CheckSpikeArm] = "Włącz te przyciski - zmieniają wczytane miasto";
            d[ButtonSpikeCreate] = "1. Utwórz testową linię autobusową";
            d[ButtonSpikeReport] = "2. Zgłoś linie testowe";
            d[ButtonSpikeRemove] = "3. Usuń linie testowe";
#endif
            return d;
        }
    }
}
