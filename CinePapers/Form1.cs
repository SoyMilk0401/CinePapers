using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CinePapers
{
    public partial class Form1 : Form
    {
        private LotteCinemaService _service = new LotteCinemaService();

        // [상태 관리 변수]
        private int _currentPage = 1;      // 현재 로드된 페이지
        private bool _isLoading = false;   // 로딩 중인지 체크
        private bool _isEnded = false;     // 데이터 끝 체크

        // [UI 컨트롤]
        private TabControl tabCategory;
        private TextBox txtSearch;
        private Button btnSearch;

        public Form1()
        {
            InitializeComponent();

            // 1. UI 초기화 (탭 + 검색창 상단 배치)
            InitializeUI();

            // 2. 스크롤 설정 (무한 스크롤)
            flowLayoutPanel1.AutoScroll = true;
            flowLayoutPanel1.Scroll += FlowLayoutPanel1_Scroll;
            flowLayoutPanel1.MouseWheel += FlowLayoutPanel1_Scroll;
        }

        // ---------------------------------------------------------
        // [UI 초기화: 상단 탭 및 검색창 배치]
        // ---------------------------------------------------------
        private void InitializeUI()
        {
            // 1. 상단 헤더 패널 (탭과 검색창을 담을 그릇)
            Panel pnlHeader = new Panel();
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Height = 35;
            pnlHeader.Padding = new Padding(0, 5, 5, 0);
            this.Controls.Add(pnlHeader);

            // 2. 검색 UI 패널 (오른쪽 정렬)
            Panel pnlSearch = new Panel();
            pnlSearch.Dock = DockStyle.Right;
            pnlSearch.Width = 220;
            pnlHeader.Controls.Add(pnlSearch);

            // 3. 검색 버튼
            btnSearch = new Button();
            btnSearch.Text = "검색";
            btnSearch.BackColor = Color.FromArgb(237, 28, 36); // 롯데 레드
            btnSearch.ForeColor = Color.White;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.Dock = DockStyle.Right;
            btnSearch.Width = 60;
            btnSearch.Cursor = Cursors.Hand;
            btnSearch.Click += BtnSearch_Click;
            pnlSearch.Controls.Add(btnSearch);

            // 4. 검색 텍스트박스
            txtSearch = new TextBox();
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Font = new Font("맑은 고딕", 10F);
            // 엔터키 입력 시 검색 실행
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnSearch_Click(s, e); };
            pnlSearch.Controls.Add(txtSearch);

            // 5. 탭 컨트롤 (나머지 왼쪽 공간 채움)
            tabCategory = new TabControl();
            tabCategory.Dock = DockStyle.Fill;

            // 카테고리 추가
            AddTab("영화", "20");
            AddTab("시사회/무대인사", "40");
            AddTab("HOT", "10");
            AddTab("제휴할인", "50");

            tabCategory.SelectedIndexChanged += TabCategory_SelectedIndexChanged;

            pnlHeader.Controls.Add(tabCategory);
            // [중요] 탭이 검색패널을 제외한 나머지 공간을 차지하도록 맨 앞으로 가져옴
            tabCategory.BringToFront();

            // 6. 메인 리스트 위치 조정 (헤더 아래)
            flowLayoutPanel1.Padding = new Padding(0, 35, 0, 0);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            pnlHeader.BringToFront();
        }

        private void AddTab(string name, string code)
        {
            TabPage page = new TabPage(name);
            page.Tag = code; // API 코드를 태그에 저장
            tabCategory.TabPages.Add(page);
        }

        // ---------------------------------------------------------
        // [이벤트 핸들러: 검색, 탭 변경, 로드, 스크롤]
        // ---------------------------------------------------------

        // 검색 버튼 클릭
        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            _currentPage = 1;
            _isEnded = false;
            _isLoading = false;
            flowLayoutPanel1.Controls.Clear();

            await LoadEventsAsync();
        }

        // 탭 변경
        private async void TabCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtSearch.Text = ""; // 탭 변경 시 검색어 초기화
            _currentPage = 1;
            _isEnded = false;
            _isLoading = false;
            flowLayoutPanel1.Controls.Clear();

            await LoadEventsAsync();
        }

        // 폼 로드
        private async void Form1_Load_1(object sender, EventArgs e)
        {
            await LoadEventsAsync();
        }

        // 데이터 로딩 (핵심 로직)
        private async Task LoadEventsAsync()
        {
            if (_isLoading || _isEnded) return;

            _isLoading = true;

            try
            {
                string currentCode = tabCategory.SelectedTab?.Tag?.ToString() ?? "20";
                string keyword = txtSearch.Text.Trim();

                // 서비스 호출 (페이지, 코드, 검색어)
                var events = await _service.GetEventsListAsync(currentCode, _currentPage,  keyword);

                if (events == null || events.Count == 0)
                {
                    _isEnded = true;
                    if (_currentPage == 1) MessageBox.Show("조회된 이벤트가 없습니다.");
                }
                else
                {
                    foreach (var item in events)
                    {
                        AddEventCard(item);
                    }
                    _currentPage++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("로드 실패: " + ex.Message);
            }
            finally
            {
                _isLoading = false;
            }
        }

        // 스크롤 바닥 감지
        private async void FlowLayoutPanel1_Scroll(object sender, EventArgs e)
        {
            int totalHeight = flowLayoutPanel1.VerticalScroll.Maximum;
            int visibleHeight = flowLayoutPanel1.ClientSize.Height;
            int currentScroll = flowLayoutPanel1.VerticalScroll.Value;

            if (currentScroll + visibleHeight >= totalHeight - 50)
            {
                await LoadEventsAsync();
            }
        }

        // ---------------------------------------------------------
        // [UI: 카드 생성]
        // ---------------------------------------------------------
        private void AddEventCard(EventItem item)
        {
            Panel card = new Panel();
            card.Size = new Size(340, 280);
            card.BorderStyle = BorderStyle.FixedSingle;
            card.Margin = new Padding(10);
            card.Cursor = Cursors.Hand;
            card.BackColor = Color.White;

            PictureBox pb = new PictureBox();
            pb.Location = new Point(10, 10);
            pb.Size = new Size(320, 200);
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Cursor = Cursors.Hand;

            if (!string.IsNullOrEmpty(item.ImageUrl))
                pb.LoadAsync(item.ImageUrl);
            card.Controls.Add(pb);

            Label lbl = new Label();
            lbl.Text = item.EventName;
            lbl.Location = new Point(10, 220);
            lbl.Size = new Size(320, 50);
            lbl.TextAlign = ContentAlignment.TopCenter;
            lbl.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            lbl.Cursor = Cursors.Hand;
            card.Controls.Add(lbl);

            EventHandler openImagePopup = (s, e) =>
            {
                var popup = new ImagePopupForm(item.EventID, item.EventName, this._service);
                popup.Show();
            };
            pb.Click += openImagePopup;
            lbl.Click += openImagePopup;
            card.Click += openImagePopup;

            flowLayoutPanel1.Controls.Add(card);
        }

        // =========================================================
        // [내부 클래스] 상세 팝업 (이미지 + 경품 조회)
        // =========================================================
        public class ImagePopupForm : Form
        {
            private string _eventId;
            private string _giftId;
            private LotteCinemaService _service;

            private Panel pnlContainer;
            private PictureBox pb;
            private Label loadingLabel;
            private Panel pnlBottom;
            private Button btnCheckStock;

            public ImagePopupForm(string eventId, string title, LotteCinemaService service)
            {
                _eventId = eventId;
                _service = service;

                // 1. 폼 설정
                this.Size = new Size(600, 800);
                this.Text = title;
                this.StartPosition = FormStartPosition.CenterScreen;
                this.Resize += ImagePopupForm_Resize;

                // 2. 하단 버튼 패널
                pnlBottom = new Panel();
                pnlBottom.Dock = DockStyle.Bottom;
                pnlBottom.Height = 60;
                pnlBottom.Padding = new Padding(10);
                pnlBottom.BackColor = Color.WhiteSmoke;
                this.Controls.Add(pnlBottom);

                // 3. 경품 확인 버튼
                btnCheckStock = new Button();
                btnCheckStock.Text = "경품 수량 확인하기";
                btnCheckStock.Dock = DockStyle.Fill;
                btnCheckStock.BackColor = Color.FromArgb(237, 28, 36);
                btnCheckStock.ForeColor = Color.White;
                btnCheckStock.Font = new Font("맑은 고딕", 12F, FontStyle.Bold);
                btnCheckStock.FlatStyle = FlatStyle.Flat;
                btnCheckStock.Enabled = false;
                btnCheckStock.Click += BtnCheckStock_Click;
                pnlBottom.Controls.Add(btnCheckStock);

                // 4. 스크롤 컨테이너
                pnlContainer = new Panel();
                pnlContainer.Dock = DockStyle.Fill;
                pnlContainer.AutoScroll = true;
                pnlContainer.BackColor = Color.Black;
                pnlContainer.Padding = new Padding(0); // 여백 제거
                this.Controls.Add(pnlContainer);
                pnlContainer.BringToFront();

                // 5. 이미지 박스
                pb = new PictureBox();
                // [핵심] DockStyle.Top을 사용하여 너비를 패널에 맞추고 가로 스크롤 제거
                pb.Dock = DockStyle.Top;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pnlContainer.Controls.Add(pb);

                // 6. 로딩 라벨
                loadingLabel = new Label();
                loadingLabel.Text = "상세 정보를 불러오는 중...";
                loadingLabel.ForeColor = Color.White;
                loadingLabel.BackColor = Color.Transparent;
                loadingLabel.AutoSize = false;
                loadingLabel.TextAlign = ContentAlignment.MiddleCenter;
                loadingLabel.Dock = DockStyle.Fill;
                pnlContainer.Controls.Add(loadingLabel);
                loadingLabel.BringToFront();

                this.Shown += ImagePopupForm_Shown;
            }

            private async void ImagePopupForm_Shown(object sender, EventArgs e)
            {
                try
                {
                    var detailItem = await _service.GetEventDetailAsync(_eventId);

                    if (detailItem != null)
                    {
                        // 경품 ID 추출
                        if (detailItem.GoodsGiftItems != null && detailItem.GoodsGiftItems.Count > 0)
                        {
                            _giftId = detailItem.GoodsGiftItems[0].FrGiftID;
                            btnCheckStock.Enabled = true;
                            btnCheckStock.Text = $"경품 수량 확인 ({detailItem.GoodsGiftItems[0].FrGiftNm})";
                        }
                        else
                        {
                            btnCheckStock.Text = "증정 경품이 없는 이벤트입니다.";
                            btnCheckStock.Enabled = false;
                        }

                        // 상세 이미지 로드
                        if (!string.IsNullOrEmpty(detailItem.ImgUrl))
                        {
                            pb.LoadCompleted += Pb_LoadCompleted;
                            pb.LoadAsync(detailItem.ImgUrl);
                        }
                        else
                        {
                            loadingLabel.Text = "이미지가 없습니다.";
                        }
                    }
                    else
                    {
                        MessageBox.Show("상세 정보를 가져올 수 없습니다.");
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("오류 발생: " + ex.Message);
                    this.Close();
                }
            }

            // 지역별 정렬 우선순위
            private int GetRegionPriority(string regionName)
            {
                switch (regionName)
                {
                    case "서울": return 1;
                    case "경기/인천": return 2;
                    case "충청/대전": return 3;
                    case "전라/광주": return 4;
                    case "경북/대구": return 5;
                    case "경남/부산/울산": return 6;
                    case "강원": return 7;
                    case "제주": return 8;
                    default: return 99;
                }
            }

            private async void BtnCheckStock_Click(object sender, EventArgs e)
            {
                string tmpText = btnCheckStock.Text;
                if (string.IsNullOrEmpty(_giftId)) return;

                try
                {
                    btnCheckStock.Enabled = false;
                    btnCheckStock.Text = "조회 중...";

                    var stockList = await _service.GetGiftStockAsync(_eventId, _giftId);

                    if (stockList != null && stockList.Count > 0)
                    {
                        var availableCinemas = stockList
                            .Where(s => s.Cnt > 0) // 재고 0 제외
                            .OrderBy(s => GetRegionPriority(s.DetailDivisionNameKR))
                            .ThenBy(s => s.SortSequence)
                            .Select(s =>
                            {
                                string suffix = s.Cnt >= 50 ? "이상" : "이하";
                                return $"[{s.DetailDivisionNameKR}] {s.CinemaNameKR}: {s.Cnt}개 {suffix}";
                            });

                        string message = string.Join("\n", availableCinemas);

                        if (string.IsNullOrEmpty(message))
                        {
                            MessageBox.Show("현재 재고가 남아있는 극장이 없습니다.");
                        }
                        else
                        {
                            MessageBox.Show(message, "경품 현황 (지역별 정렬)");
                        }
                    }
                    else
                    {
                        MessageBox.Show("경품 수량 정보를 가져올 수 없습니다.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("수량 조회 오류: " + ex.Message);
                }
                finally
                {
                    btnCheckStock.Enabled = true;
                    btnCheckStock.Text = tmpText;
                }
            }

            private void Pb_LoadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
            {
                loadingLabel.Visible = false;
                if (e.Error == null && pb.Image != null)
                {
                    ResizeImageToFitWidth();
                }
            }

            private void ImagePopupForm_Resize(object sender, EventArgs e)
            {
                ResizeImageToFitWidth();
            }

            private void ResizeImageToFitWidth()
            {
                if (pb.Image == null) return;

                // [핵심] Dock=Top 덕분에 너비는 패널과 동일함. 높이만 비율대로 계산.
                int currentWidth = pb.Width;
                float ratio = (float)pb.Image.Height / pb.Image.Width;
                pb.Height = (int)(currentWidth * ratio);
            }
        }
    }
}