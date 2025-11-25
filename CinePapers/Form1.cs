using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CinePapers.Controls; // 분리된 파일 사용
using CinePapers.Forms;    // 분리된 파일 사용
using CinePapers.Models.Common;
using CinePapers.Services; // 분리된 파일 사용

namespace CinePapers
{
    public partial class Form1 : Form
    {
        private ICinemaService _currentService;
        private ComboBox _cboCinema;
        private TabControl _tabCategory;
        private TextBox _txtSearch;
        private Button _btnSearch;

        private int _currentPage = 1;
        private bool _isLoading = false;
        private bool _isEnded = false;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomUI();

            // 초기 실행
            if (_cboCinema.Items.Count > 0)
                _cboCinema.SelectedIndex = 0;
        }

        private void InitializeCustomUI()
        {
            // 상단 헤더
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(5) };
            this.Controls.Add(pnlHeader);

            // 1. 콤보박스
            _cboCinema = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Dock = DockStyle.Right, Font = new Font("맑은 고딕", 10F) };
            foreach (var s in CinemaServiceManager.GetAvailableServices()) _cboCinema.Items.Add(s);
            _cboCinema.DisplayMember = "CinemaName";
            _cboCinema.SelectedIndexChanged += OnCinemaChanged;
            pnlHeader.Controls.Add(_cboCinema);

            // 2. 검색창
            Panel pnlSearch = new Panel { Dock = DockStyle.Right, Width = 250 };
            pnlHeader.Controls.Add(pnlSearch);
            _btnSearch = new Button { Text = "검색", Dock = DockStyle.Right };
            _btnSearch.Click += (s, e) => _ = ReloadEventsAsync();
            pnlSearch.Controls.Add(_btnSearch);
            _txtSearch = new TextBox { Dock = DockStyle.Fill };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _ = ReloadEventsAsync(); };
            pnlSearch.Controls.Add(_txtSearch);

            // 3. 탭 컨트롤
            _tabCategory = new TabControl { Dock = DockStyle.Fill };
            _tabCategory.SelectedIndexChanged += (s, e) => _ = ReloadEventsAsync();
            pnlHeader.Controls.Add(_tabCategory);
            _tabCategory.BringToFront();

            // 4. 리스트 (Designer의 flowLayoutPanel1 사용)
            flowLayoutPanel1.Padding = new Padding(0, 40, 0, 0);
            pnlHeader.BringToFront();
            flowLayoutPanel1.Scroll += OnListScroll;
            flowLayoutPanel1.MouseWheel += OnListScroll;
        }

        // [버그 수정] 영화관 변경 로직 개선
        private void OnCinemaChanged(object sender, EventArgs e)
        {
            _currentService = _cboCinema.SelectedItem as ICinemaService;
            if (_currentService == null) return;

            // ★ 중요: 탭 변경 중 이벤트가 불필요하게 발생하는 것을 방지하기 위해 이벤트 제거
            _tabCategory.SelectedIndexChanged -= TabSelectionHandler;

            _tabCategory.TabPages.Clear();
            foreach (var cat in _currentService.GetCategories())
            {
                TabPage page = new TabPage(cat.Key) { Tag = cat.Value };
                _tabCategory.TabPages.Add(page);
            }

            // 첫 번째 탭 선택 (데이터가 있을 경우)
            if (_tabCategory.TabPages.Count > 0)
                _tabCategory.SelectedIndex = 0;

            // ★ 중요: 이벤트 다시 연결 및 강제 로드 실행
            // 이렇게 하면 '이미 0번 인덱스라 이벤트가 안 터지는 문제'와 '중복 로드'를 모두 해결합니다.
            _tabCategory.SelectedIndexChanged += TabSelectionHandler;
            _ = ReloadEventsAsync();
        }

        // 람다식 대신 메서드로 분리하여 이벤트 구독/해지 용이하게 변경
        private void TabSelectionHandler(object sender, EventArgs e)
        {
            _ = ReloadEventsAsync();
        }

        private async Task ReloadEventsAsync()
        {
            _currentPage = 1;
            _isEnded = false;
            flowLayoutPanel1.Controls.Clear();
            await LoadEventsAsync();
        }

        private async Task LoadEventsAsync()
        {
            if (_isLoading || _isEnded || _currentService == null) return;

            // 선택된 탭이 없으면 중단
            if (_tabCategory.SelectedTab == null) return;

            _isLoading = true;
            try
            {
                string categoryCode = _tabCategory.SelectedTab.Tag.ToString();
                string keyword = _txtSearch.Text.Trim();

                var events = await _currentService.GetEventsListAsync(categoryCode, _currentPage, keyword);

                if (events == null || events.Count == 0)
                {
                    _isEnded = true;
                    if (_currentPage == 1) MessageBox.Show("조회된 이벤트가 없습니다.");
                }
                else
                {
                    flowLayoutPanel1.SuspendLayout();
                    foreach (var item in events)
                    {
                        // 분리된 컨트롤 사용
                        var card = new EventCardControl(item);
                        card.CardClicked += OnCardClicked;
                        flowLayoutPanel1.Controls.Add(card);
                    }
                    flowLayoutPanel1.ResumeLayout();
                    _currentPage++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("로드 오류: " + ex.Message);
            }
            finally { _isLoading = false; }
        }

        private void OnCardClicked(object sender, CinemaEventItem item)
        {
            // 분리된 팝업 폼 사용
            var popup = new EventDetailPopup(item.EventId, item.Title, _currentService);
            popup.Show();
        }

        private async void OnListScroll(object sender, EventArgs e)
        {
            if (flowLayoutPanel1.VerticalScroll.Value + flowLayoutPanel1.ClientSize.Height >= flowLayoutPanel1.VerticalScroll.Maximum - 50)
                await LoadEventsAsync();
        }
    }
}