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
        public const string SpikeConfirmTitle = "spike.confirm.title";
        public const string SpikeConfirmCreate = "spike.confirm.create";
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
            d[CheckSpikeArm] = "Arm the create button; every click still asks for confirmation";
            d[ButtonSpikeCreate] = "1. Create a test bus line";
            d[ButtonSpikeReport] = "2. Report test lines";
            d[ButtonSpikeRemove] = "3. Remove test lines (disabled: cannot undo)";
            d[SpikeConfirmTitle] = "Create test bus line?";
            d[SpikeConfirmCreate] = "This creates one APTP Test bus line with 3 to 6 stops on existing roads. The line can later be deleted in the game's public transport controls, but simulation changes while it runs cannot be rolled back. Back up your save before continuing. Continue?";
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
            d[CheckSpikeArm] = "啟用建立按鈕；每次按下仍會再次要求確認";
            d[ButtonSpikeCreate] = "1. 建立測試公車路線";
            d[ButtonSpikeReport] = "2. 回報測試路線";
            d[ButtonSpikeRemove] = "3. 移除測試路線（已停用：無法還原）";
            d[SpikeConfirmTitle] = "要建立測試公車路線嗎？";
            d[SpikeConfirmCreate] = "這會在現有道路上建立一條有 3 至 6 個站點的 APTP Test 公車路線。稍後可在遊戲大眾運輸介面刪除該路線，但路線運行期間造成的模擬變化無法還原。繼續前請先備份存檔。要繼續嗎？";
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
            d[CheckSpikeArm] = "启用创建按钮；每次点击仍会再次要求确认";
            d[ButtonSpikeCreate] = "1. 创建测试公交线路";
            d[ButtonSpikeReport] = "2. 报告测试线路";
            d[ButtonSpikeRemove] = "3. 移除测试线路（已停用：无法还原）";
            d[SpikeConfirmTitle] = "要创建测试公交线路吗？";
            d[SpikeConfirmCreate] = "这会在现有道路上创建一条包含 3 至 6 个站点的 APTP Test 公交线路。之后可在游戏公共交通界面删除该线路，但线路运行期间造成的模拟变化无法还原。继续前请先备份存档。要继续吗？";
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
            d[CheckSpikeArm] = "作成ボタンを有効にします。クリックするたびに確認が表示されます";
            d[ButtonSpikeCreate] = "1. テストバス路線を作成";
            d[ButtonSpikeReport] = "2. テスト路線を報告";
            d[ButtonSpikeRemove] = "3. テスト路線を削除（復元不可のため無効）";
            d[SpikeConfirmTitle] = "テスト用バス路線を作成しますか？";
            d[SpikeConfirmCreate] = "既存の道路に3～6か所の停留所を持つ APTP Test のバス路線を1本作成します。路線は後でゲームの公共交通画面から削除できますが、運行中に起きたシミュレーションの変化は元に戻せません。続行する前にセーブデータをバックアップしてください。続行しますか？";
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
            d[CheckSpikeArm] = "생성 버튼을 활성화합니다. 클릭할 때마다 다시 확인합니다";
            d[ButtonSpikeCreate] = "1. 테스트 버스 노선 생성";
            d[ButtonSpikeReport] = "2. 테스트 노선 보고";
            d[ButtonSpikeRemove] = "3. 테스트 노선 제거 (복구 불가로 비활성화)";
            d[SpikeConfirmTitle] = "테스트 버스 노선을 생성할까요?";
            d[SpikeConfirmCreate] = "기존 도로에 정류장 3~6개가 있는 APTP Test 버스 노선 한 개를 생성합니다. 노선은 게임의 대중교통 화면에서 나중에 삭제할 수 있지만, 운행 중 발생한 시뮬레이션 변화는 되돌릴 수 없습니다. 계속하기 전에 저장 파일을 백업하세요. 계속할까요?";
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
            d[CheckSpikeArm] = "Erstellen-Schaltfläche aktivieren; jeder Klick erfordert erneut eine Bestätigung";
            d[ButtonSpikeCreate] = "1. Testbuslinie erstellen";
            d[ButtonSpikeReport] = "2. Testlinien melden";
            d[ButtonSpikeRemove] = "3. Testlinien entfernen (deaktiviert: nicht wiederherstellbar)";
            d[SpikeConfirmTitle] = "Testbuslinie erstellen?";
            d[SpikeConfirmCreate] = "Es wird eine APTP-Testbuslinie mit 3 bis 6 Haltestellen auf vorhandenen Straßen erstellt. Die Linie kann später in der ÖPNV-Ansicht des Spiels gelöscht werden, aber Simulationsänderungen während des Betriebs lassen sich nicht rückgängig machen. Sichern Sie Ihren Spielstand, bevor Sie fortfahren. Fortfahren?";
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
            d[CheckSpikeArm] = "Activer le bouton de création ; chaque clic demandera une nouvelle confirmation";
            d[ButtonSpikeCreate] = "1. Créer une ligne de bus de test";
            d[ButtonSpikeReport] = "2. Signaler les lignes de test";
            d[ButtonSpikeRemove] = "3. Supprimer les lignes de test (désactivé : irréversible)";
            d[SpikeConfirmTitle] = "Créer une ligne de bus de test ?";
            d[SpikeConfirmCreate] = "Cela créera une ligne de bus APTP Test avec 3 à 6 arrêts sur des routes existantes. La ligne pourra être supprimée dans l'interface de transport du jeu, mais les changements de simulation survenus pendant son fonctionnement ne pourront pas être annulés. Sauvegardez votre partie avant de continuer. Continuer ?";
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
            d[CheckSpikeArm] = "Activar el botón de creación; cada clic volverá a pedir confirmación";
            d[ButtonSpikeCreate] = "1. Crear una línea de autobús de prueba";
            d[ButtonSpikeReport] = "2. Informar de las líneas de prueba";
            d[ButtonSpikeRemove] = "3. Eliminar líneas de prueba (desactivado: irreversible)";
            d[SpikeConfirmTitle] = "¿Crear una línea de autobús de prueba?";
            d[SpikeConfirmCreate] = "Esto creará una línea de autobús APTP Test con 3 a 6 paradas en carreteras existentes. Más tarde se puede eliminar desde la interfaz de transporte público del juego, pero no se pueden revertir los cambios de simulación producidos mientras funcione. Haz una copia de seguridad de la partida antes de continuar. ¿Continuar?";
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
            d[CheckSpikeArm] = "Ativar o botão de criação; cada clique pedirá confirmação novamente";
            d[ButtonSpikeCreate] = "1. Criar uma linha de ônibus de teste";
            d[ButtonSpikeReport] = "2. Relatar as linhas de teste";
            d[ButtonSpikeRemove] = "3. Remover linhas de teste (desativado: irreversível)";
            d[SpikeConfirmTitle] = "Criar uma linha de ônibus de teste?";
            d[SpikeConfirmCreate] = "Isso criará uma linha de ônibus APTP Test com 3 a 6 paradas em vias existentes. Depois, ela poderá ser removida na interface de transporte público do jogo, mas as mudanças na simulação ocorridas enquanto ela funcionar não poderão ser revertidas. Faça backup do seu jogo salvo antes de continuar. Continuar?";
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
            d[CheckSpikeArm] = "Включить кнопку создания; каждое нажатие потребует нового подтверждения";
            d[ButtonSpikeCreate] = "1. Создать тестовый автобусный маршрут";
            d[ButtonSpikeReport] = "2. Отчёт по тестовым маршрутам";
            d[ButtonSpikeRemove] = "3. Удалить тестовые маршруты (отключено: восстановление невозможно)";
            d[SpikeConfirmTitle] = "Создать тестовый автобусный маршрут?";
            d[SpikeConfirmCreate] = "Будет создан один автобусный маршрут APTP Test с 3–6 остановками на существующих дорогах. Позже маршрут можно удалить в меню транспорта игры, но изменения симуляции, произошедшие во время его работы, отменить нельзя. Перед продолжением создайте резервную копию сохранения. Продолжить?";
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
            d[CheckSpikeArm] = "Aktywuj przycisk tworzenia; każde kliknięcie ponownie wyświetli prośbę o potwierdzenie";
            d[ButtonSpikeCreate] = "1. Utwórz testową linię autobusową";
            d[ButtonSpikeReport] = "2. Zgłoś linie testowe";
            d[ButtonSpikeRemove] = "3. Usuń linie testowe (wyłączone: brak możliwości przywrócenia)";
            d[SpikeConfirmTitle] = "Czy utworzyć testową linię autobusową?";
            d[SpikeConfirmCreate] = "Utworzona zostanie jedna linia autobusowa APTP Test z 3–6 przystankami na istniejących drogach. Później można ją usunąć w interfejsie transportu publicznego gry, ale zmian symulacji, które nastąpią podczas jej działania, nie można cofnąć. Przed kontynuowaniem wykonaj kopię zapasową zapisu gry. Kontynuować?";
#endif
            return d;
        }
    }
}
