using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RailwayApp
{
    public partial class MainForm : Form
    {
        private readonly Station _station = new Station();
        private readonly BindingSource _bindingSource = new BindingSource();
        private string _sortColumn = nameof(Tariff.Direction);
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;
        public MainForm()
        {
            InitializeComponent();
            SetupDataGridView();
            InitializeSampleData();
        }

        private void SetupDataGridView()
        {
            dataGridView.AutoGenerateColumns = false;
            dataGridView.DataSource = _bindingSource;

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Tariff.Direction),
                HeaderText = "Направление",
                Name = "colDirection"
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Tariff.BaseCost),
                HeaderText = "Базовая стоимость",
                Name = "colBaseCost",
                DefaultCellStyle = { Format = "C" },
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Tariff.FinalCost),
                HeaderText = "Итоговая стоимость",
                Name = "colFinalCost",
                DefaultCellStyle = { Format = "C" },
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(Tariff.DiscountType),
                HeaderText = "Тип скидки",
                Name = "colDiscountType"
            });

            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.Automatic;
            }
        }

        private void ApplySorting()
        {
            var tariffs = _station.GetAllTariffs().AsQueryable();

            switch (_sortColumn)
            {
                case nameof(Tariff.Direction):
                    tariffs = _sortDirection == ListSortDirection.Ascending
                        ? tariffs.OrderBy(t => t.Direction, StringComparer.CurrentCulture)
                        : tariffs.OrderByDescending(t => t.Direction, StringComparer.CurrentCulture);
                    break;

                case nameof(Tariff.BaseCost):
                    tariffs = _sortDirection == ListSortDirection.Ascending
                        ? tariffs.OrderBy(t => t.BaseCost)
                        : tariffs.OrderByDescending(t => t.BaseCost);
                    break;

                case nameof(Tariff.FinalCost):
                    tariffs = _sortDirection == ListSortDirection.Ascending
                        ? tariffs.OrderBy(t => t.FinalCost)
                        : tariffs.OrderByDescending(t => t.FinalCost);
                    break;

                case nameof(Tariff.DiscountType):
                    tariffs = _sortDirection == ListSortDirection.Ascending
                        ? tariffs.OrderBy(t => t.GetDiscountPercentForSorting())
                        : tariffs.OrderByDescending(t => t.GetDiscountPercentForSorting());
                    break;
            }

            _bindingSource.DataSource = tariffs.ToList();
            UpdateSortIndicators();
        }

        private void dataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // Получаем имя свойства для сортировки
            var column = dataGridView.Columns[e.ColumnIndex];
            string propertyName = column.DataPropertyName;

            // Меняем направление, если кликнули по тому же столбцу
            if (_sortColumn == propertyName)
            {
                _sortDirection = _sortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }
            else
            {
                _sortColumn = propertyName;
                _sortDirection = ListSortDirection.Ascending;
            }

            // Применяем сортировку
            ApplySorting();

            // Обновляем стрелки в заголовках
            UpdateSortIndicators();
        }

        private void UpdateSortIndicators()
        {
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            var sortedColumn = dataGridView.Columns
                .Cast<DataGridViewColumn>()
                .FirstOrDefault(c => c.DataPropertyName == _sortColumn);

            if (sortedColumn != null)
            {
                sortedColumn.HeaderCell.SortGlyphDirection =
                    _sortDirection == ListSortDirection.Ascending
                        ? SortOrder.Ascending
                        : SortOrder.Descending;
            }
        }

        private void InitializeSampleData()
        {
            _station.AddTariff("Москва", 5000);
            _station.AddTariff("Санкт-Петербург", 3000, 10);
            _station.AddTariff("Казань", 2000, 5);
            ApplySorting();
        }

        private void RefreshGrid()
        {
            ApplySorting();
            dataGridView.ClearSelection();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            using (var form = new EditForm(_station))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    RefreshGrid();
                }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите тариф для редактирования", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedTariff = (Tariff)dataGridView.SelectedRows[0].DataBoundItem;

            using (var form = new EditForm(_station, selectedTariff))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    ApplySorting();
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите тариф для удаления", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Удалить выбранный тариф?", "Подтверждение",
       MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                var tariffToRemove = (Tariff)dataGridView.SelectedRows[0].DataBoundItem;
                var allTariffs = _station.GetAllTariffs();
                int index = -1;

                index = allTariffs.IndexOf(tariffToRemove);

                if (index == -1)
                {
                    index = allTariffs.FindIndex(t =>
                        string.Equals(t.Direction, tariffToRemove.Direction, StringComparison.OrdinalIgnoreCase) &&
                        Math.Abs(t.BaseCost - tariffToRemove.BaseCost) < 0.01 &&
                        t.DiscountType == tariffToRemove.DiscountType
                    );
                }
                if (index == -1)
                {
                    throw new Exception("Не удалось найти тариф для удаления");
                }
                _station.RemoveTariff(index);
                ApplySorting();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnFindMin_Click(object sender, EventArgs e)
        {
            try
            {
                var minDirections = _station.FindAllMinCostDirections();
                string message;
                if (minDirections.Count == 1)
                {
                    message = $"Направление с минимальной стоимостью:\n{minDirections[0]}";
                }
                else
                {
                    message = $"Направления с минимальной стоимостью ({minDirections.Count} шт.):\n" +
                              string.Join("\n", minDirections);
                }

                MessageBox.Show(message, "Результат",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                dialog.Title = "Сохранить тарифы";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        FileService.SaveToFile(_station, dialog.FileName);
                        MessageBox.Show("Данные успешно сохранены!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                dialog.Title = "Загрузить тарифы";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _station.Clear();
                        FileService.LoadFromFile(_station, dialog.FileName);
                        ApplySorting();
                        MessageBox.Show("Данные успешно загружены!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}